/*
 * NexAlbum pro geçiş efektleri — HTML prova karşılığı (Karar 23).
 * Hesap, C# TransitionEffects.Compute ile birebir aynıdır (testle denetlenir: EffectsScriptTests).
 * Alan: genişlik = en/boy oranı, yükseklik = 1. Çizim: canvas 2D (paint).
 * GPL-3.0-or-later. Dış bağımlılık yok.
 */
var NexEffects = (function () {
  "use strict";

  var SEGMENTS = 72;
  var WHITE = [255, 255, 255], BLACK = [0, 0, 0], AMBER = [255, 170, 90], ROSE = [255, 120, 140], EMBER = [255, 140, 40];

  var INFO = [
    ["aperture", "Aperture", "Diyafram", true],
    ["diaphragm", "Diyafram kanatları", "Diyafram", false],
    ["doubleIris", "Çift iris", "Diyafram", false],
    ["diamondReveal", "Elmas açılış", "Diyafram", false],
    ["iris", "Dairesel iris", "Diyafram", false],
    ["rackFocus", "Netlik kaydırma", "Odak", false],
    ["focusShift", "Odak kaydırma", "Odak", false],
    ["tiltShift", "Tilt-shift", "Odak", false],
    ["depthFocus", "Derinlikten", "Odak", false],
    ["lightLeak", "Işık sızıntısı", "Işık ve film", false],
    ["filmBurn", "Film yanığı", "Işık ve film", true],
    ["flash", "Flaş", "Işık ve film", false],
    ["projector", "Projektör", "Işık ve film", false],
    ["polaroidDevelop", "Polaroid banyosu", "Işık ve film", false],
    ["crossfade", "Çapraz geçiş", "Klasik", false],
    ["dipToBlack", "Siyaha geçiş", "Klasik", false],
    ["dipToWhite", "Beyaza geçiş", "Klasik", false],
    ["cut", "Kesme", "Klasik", false],
    ["curtain", "Perde", "Maske", false],
    ["split", "Yarık", "Maske", false],
    ["blinds", "Panjur", "Maske", false],
    ["diagonalWipe", "Çapraz silme", "Maske", false],
    ["mosaic", "Mozaik", "Maske", false],
    ["honeycomb", "Petek", "Maske", false],
    ["inkBlot", "Mürekkep lekesi", "Maske", true],
    ["parallax", "Paralaks", "Hareket", false],
    ["whipPan", "Hızlı kaydırma", "Hareket", false],
    ["pushLeft", "Sola itme", "Hareket", false],
    ["pushRight", "Sağa itme", "Hareket", false],
    ["pushUp", "Yukarı itme", "Hareket", false],
    ["zoomThrough", "Yakınlaşarak", "Hareket", false],
    ["cardStack", "Kart yığını", "Albüm", false],
    ["pageTurn", "Sayfa çevirme", "Albüm", false]
  ].map(function (e) { return { kind: e[0], name: e[1], group: e[2], usesOrigin: e[3] }; });

  function info(kind) {
    for (var i = 0; i < INFO.length; i++) { if (INFO[i].kind === kind) { return INFO[i]; } }
    return INFO[0];
  }

  function durationFactor(kind) {
    switch (kind) {
      case "flash": case "whipPan": case "cut": return 0.6;
      case "projector": return 0.8;
      case "polaroidDevelop": case "lightLeak": case "pageTurn": return 1.4;
      case "aperture": case "diaphragm": case "inkBlot": case "honeycomb": case "mosaic": return 1.2;
      default: return 1;
    }
  }

  function mulberry32(seed) {
    var a = seed >>> 0;
    return function () {
      a = (a + 0x6d2b79f5) >>> 0;
      var t = a;
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  function clamp(v, a, b) { return Math.min(b, Math.max(a, v)); }
  function smooth(p) { return p * p * (3 - 2 * p); }
  function smoothStep(a, b, p) { return smooth(clamp((p - a) / (b - a), 0, 1)); }
  function outCubic(p) { return 1 - Math.pow(1 - p, 3); }
  function inOutCubic(p) { return p < 0.5 ? 4 * p * p * p : 1 - Math.pow(-2 * p + 2, 3) / 2; }
  function irisEase(p) { return p < 0.5 ? 8 * Math.pow(p, 4) : 1 - Math.pow(-2 * p + 2, 4) / 2; }
  function inOutExpo(p) { return p <= 0 ? 0 : p >= 1 ? 1 : p < 0.5 ? Math.pow(2, 20 * p - 10) / 2 : (2 - Math.pow(2, -20 * p + 10)) / 2; }
  function outBack(p) { var c1 = 1.2, c3 = c1 + 1; return 1 + c3 * Math.pow(p - 1, 3) + c1 * Math.pow(p - 1, 2); }
  function coverRadius(x, y, a) { return Math.sqrt(Math.pow(Math.max(x, a - x), 2) + Math.pow(Math.max(y, 1 - y), 2)); }

  function layer(a, o) {
    var l = { scale: 1, scaleX: 1, scaleY: 1, offsetX: 0, offsetY: 0, rotation: 0, pivotX: a / 2, pivotY: 0.5, opacity: 1, blur: 0, saturation: 1, brightness: 1, shadow: 0 };
    if (o) { for (var k in o) { if (Object.prototype.hasOwnProperty.call(o, k)) { l[k] = o[k]; } } }
    return l;
  }

  function frame(from, to, mask, overlays, toOnTop) {
    return { from: from, to: to, mask: mask || null, toOnTop: toOnTop !== false, overlays: overlays || [] };
  }

  function circle(cx, cy, r) {
    var pts = [];
    for (var i = 0; i < SEGMENTS; i++) { var t = 2 * Math.PI * i / SEGMENTS; pts.push([cx + r * Math.cos(t), cy + r * Math.sin(t)]); }
    return pts;
  }

  function regular(cx, cy, r, sides, rot) {
    var pts = [];
    for (var i = 0; i < sides; i++) { var t = 2 * Math.PI * i / sides + rot * Math.PI / 180; pts.push([cx + r * Math.cos(t), cy + r * Math.sin(t)]); }
    return pts;
  }

  function rect(x, y, w, h) { return [[x, y], [x + w, y], [x + w, y + h], [x, y + h]]; }

  function blob(cx, cy, radius, rough, rnd) {
    var f1 = 3 + Math.floor(rnd() * 3), f2 = 7 + Math.floor(rnd() * 4), f3 = 13 + Math.floor(rnd() * 5);
    var p1 = rnd() * Math.PI * 2, p2 = rnd() * Math.PI * 2, p3 = rnd() * Math.PI * 2;
    var pts = [];
    for (var i = 0; i < SEGMENTS; i++) {
      var t = 2 * Math.PI * i / SEGMENTS;
      var wob = 0.6 * Math.sin(f1 * t + p1) + 0.3 * Math.sin(f2 * t + p2) + 0.1 * Math.sin(f3 * t + p3);
      var rr = radius * (1 + rough * wob);
      pts.push([cx + rr * Math.cos(t), cy + rr * Math.sin(t)]);
    }
    return pts;
  }

  function shuffle(n, rnd) {
    var order = [];
    for (var i = 0; i < n; i++) { order.push(i); }
    for (var k = n - 1; k > 0; k--) { var j = Math.floor(rnd() * (k + 1)); var tmp = order[k]; order[k] = order[j]; order[j] = tmp; }
    return order;
  }

  function ring(cx, cy, r, w, alpha, c) { return { type: "ring", cx: cx, cy: cy, r: r, width: w, alpha: alpha, color: c }; }
  function solid(c, alpha) { return { type: "solid", color: c, alpha: alpha }; }
  function glow(cx, cy, r, c, alpha) { return { type: "glow", cx: cx, cy: cy, r: r, color: c, alpha: alpha }; }
  function band(x0, x1, a0, a1, c) { return { type: "band", x0: x0, x1: x1, alpha0: a0, alpha1: a1, color: c }; }

  /** Geçişin p anındaki hâli (C# TransitionEffects.Compute karşılığı). origin: [x, y] ya da null. */
  function compute(kind, p, aspect, origin, seed) {
    p = clamp(isFinite(p) ? p : 0, 0, 1);
    var a = aspect, cx = a / 2;
    var rnd = mulberry32(seed === undefined ? 1 : seed);
    var o = origin || (info(kind).usesOrigin ? [a * (0.25 + 0.5 * rnd()), 0.3 + 0.4 * rnd()] : [cx, 0.5]);
    var cover = coverRadius(o[0], o[1], a);
    var e = smooth(p);
    var L = function (x) { return layer(a, x); };
    var k, r, half, list, i;

    switch (kind) {
      case "cut": return frame(L({ opacity: p < 0.5 ? 1 : 0 }), L({ opacity: p < 0.5 ? 0 : 1 }));
      case "dipToBlack": case "dipToWhite":
        return frame(L({ opacity: p < 0.5 ? 1 : 0 }), L({ opacity: p < 0.5 ? 0 : 1 }), null, [solid(kind === "dipToBlack" ? BLACK : WHITE, 1 - Math.abs(p * 2 - 1))]);
      case "focusShift": return frame(L({ blur: 0.03 * e }), L({ opacity: e, blur: 0.03 * (1 - e) }));
      case "iris": return frame(L(), L(), [circle(cx, 0.5, e * coverRadius(cx, 0.5, a))]);
      case "pushLeft": case "pushRight": case "pushUp":
        var dx = kind === "pushLeft" ? -a : kind === "pushRight" ? a : 0;
        var dy = kind === "pushUp" ? -1 : 0;
        return frame(L({ offsetX: dx * e, offsetY: dy * e }), L({ offsetX: -dx * (1 - e), offsetY: -dy * (1 - e) }));
      case "zoomThrough":
        return frame(L({ scale: 1 + e * 0.5, blur: 0.02 * e }), L({ scale: 0.85 + e * 0.15, opacity: e, blur: 0.02 * (1 - e) }));
      case "aperture":
        k = irisEase(p); r = k * cover;
        var ringAlpha = p < 0.7 ? 1 : 1 - (p - 0.7) / 0.3;
        return frame(L(), L({ scale: 1.18 - 0.18 * outCubic(Math.min(1, p / 0.8)), pivotX: o[0], pivotY: o[1] }), [circle(o[0], o[1], r)],
          [ring(o[0], o[1], r, 0.0022, ringAlpha * (r > 0.002 ? 1 : 0), WHITE)]);
      case "diaphragm":
        k = irisEase(p); r = k * cover * 1.16;
        return frame(L({ brightness: 1 - 0.3 * k }), L({ scale: 1.08 - 0.08 * k }), [regular(cx, 0.5, r, 6, (1 - k) * 60)], [ring(cx, 0.5, r * 0.87, 0.0016, (1 - k) * 0.8, WHITE)]);
      case "doubleIris":
        k = irisEase(p);
        var c1 = [a * 0.3, 0.4], c2 = [a * 0.7, 0.6];
        r = k * Math.max(coverRadius(c1[0], c1[1], a), coverRadius(c2[0], c2[1], a)) * 0.9;
        return frame(L(), L(), [circle(c1[0], c1[1], r), circle(c2[0], c2[1], r)]);
      case "diamondReveal":
        var d = irisEase(p) * (cx + 0.5) * 1.02;
        return frame(L(), L(), [[[cx - d, 0.5], [cx, 0.5 - d], [cx + d, 0.5], [cx, 0.5 + d]]]);
      case "rackFocus":
        return frame(L({ blur: 0.05 * e, scale: 1 + 0.04 * e }), L({ opacity: smoothStep(0.3, 0.8, p), blur: 0.05 * (1 - e), scale: 1.04 - 0.04 * e }));
      case "tiltShift":
        half = 0.5 * e;
        return frame(L({ blur: 0.03 * e }), L({ blur: 0.02 * (1 - e) }), [rect(-1, 0.5 - half, a + 2, half * 2)]);
      case "depthFocus":
        return frame(L({ scale: 1 + 0.15 * e, blur: 0.03 * e }), L({ scale: 0.6 + 0.4 * outCubic(p), blur: 0.06 * (1 - e), opacity: smoothStep(0, 0.6, p), shadow: 0.6 * (1 - e) }));
      case "lightLeak":
        var bell = Math.sin(Math.PI * p);
        return frame(L({ brightness: 1 + 0.25 * bell }), L({ opacity: smoothStep(0.35, 0.75, p), brightness: 1 + 0.25 * bell }), null,
          [glow(-0.3 * a + 1.6 * a * p, 0.3, 0.9, AMBER, bell * 0.85), glow(1.2 * a - 1.4 * a * p, 0.8, 0.7, ROSE, bell * 0.6)]);
      case "filmBurn":
        r = e * cover * 1.25;
        return frame(L({ brightness: 1 + 0.6 * e }), L(), [blob(o[0], o[1], r, 0.18, rnd)], [glow(o[0], o[1], r * 1.3 + 0.1, EMBER, Math.sin(Math.PI * p) * 0.9)]);
      case "flash":
        var fl = p < 0.5 ? Math.pow(p / 0.5, 3) : 1 - Math.pow((p - 0.5) / 0.5, 0.6);
        return frame(L({ opacity: p < 0.5 ? 1 : 0 }), L({ opacity: p < 0.5 ? 0 : 1, brightness: 1 + 0.5 * (1 - e) }), null, [solid(WHITE, fl)]);
      case "projector":
        var step = Math.floor(p * 12);
        var showNew = p >= 0.6 || (p >= 0.25 && step % 2 === 1);
        var jitter = 0.012 * Math.sin(p * 50) * (1 - e);
        var flicker = p > 0.15 && p < 0.75 ? 0.25 * (step % 2) : 0;
        return frame(L({ opacity: showNew ? 0 : 1, offsetY: jitter, brightness: 1 - flicker }), L({ opacity: showNew ? 1 : 0, offsetY: -jitter, brightness: 1 - flicker }));
      case "polaroidDevelop":
        return frame(L(), L({ opacity: smoothStep(0, 0.3, p), saturation: e * e, brightness: 1 + 1.2 * (1 - e), scale: 0.96 + 0.04 * e, shadow: 0.4 * (1 - e) }));
      case "crossfade": return frame(L(), L({ opacity: e }));
      case "curtain":
        half = inOutCubic(p) * cx;
        return frame(L({ brightness: 1 - 0.35 * e }), L(), [rect(cx - half, -1, half * 2, 3)],
          half > 0.001 ? [band(cx - half - 0.04, cx - half, 0, 0.45 * (1 - e), BLACK), band(cx + half, cx + half + 0.04, 0.45 * (1 - e), 0, BLACK)] : []);
      case "split":
        half = inOutCubic(p) * 0.5;
        return frame(L({ brightness: 1 - 0.4 * e }), L({ scale: 1.15 - 0.15 * e }), [rect(-1, 0.5 - half, a + 2, half * 2)]);
      case "blinds":
        list = [];
        for (i = 0; i < 8; i++) {
          var local = inOutCubic(clamp((p - i * 0.05) / 0.65, 0, 1));
          if (local > 0) { list.push(rect(-1, i / 8, a + 2, local / 8)); }
        }
        return frame(L(), L(), list);
      case "diagonalWipe":
        var c = -0.5 + inOutCubic(p) * (a + 1);
        return frame(L(), L(), [[[-1, -1], [c + 0.5, -1], [c - 0.5, 2], [-1, 2]]]);
      case "mosaic":
        var rows = 6, cols = Math.ceil(a * rows), size = 1 / rows;
        var order = shuffle(cols * rows, rnd);
        list = [];
        for (i = 0; i < order.length; i++) {
          var delay = order[i] / order.length * 0.6;
          var lm = outCubic(clamp((p - delay) / 0.4, 0, 1));
          if (lm <= 0) { continue; }
          var col = i % cols, row = Math.floor(i / cols), s = size * lm * 1.02;
          list.push(rect(col * size + (size - s) / 2, row * size + (size - s) / 2, s, s));
        }
        return frame(L(), L(), list);
      case "honeycomb":
        var R = 0.09, hw = Math.sqrt(3) * R, maxD = coverRadius(cx, 0.5, a);
        list = [];
        for (var hr = -1; hr * 1.5 * R < 1 + R; hr++) {
          for (var hc = -1; hc * hw < a + hw; hc++) {
            var hx = hc * hw + (hr % 2 === 0 ? 0 : hw / 2), hy = hr * 1.5 * R;
            var dist = Math.sqrt((hx - cx) * (hx - cx) + (hy - 0.5) * (hy - 0.5)) / maxD;
            var lh = outCubic(clamp((p - dist * 0.55) / 0.45, 0, 1));
            if (lh > 0) { list.push(regular(hx, hy, R * 1.04 * lh, 6, 30)); }
          }
        }
        return frame(L(), L(), list);
      case "inkBlot":
        list = [];
        var main = outCubic(p) * cover * 1.4;
        if (main > 0) { list.push(blob(o[0], o[1], main, 0.25, rnd)); }
        for (i = 0; i < 3; i++) {
          var bx = a * (0.1 + 0.8 * rnd()), by = 0.1 + 0.8 * rnd();
          var lb = outCubic(clamp((p - 0.1 - i * 0.12) / 0.6, 0, 1));
          var rb = lb * coverRadius(bx, by, a) * 0.8;
          if (rb > 0) { list.push(blob(bx, by, rb, 0.3, rnd)); }
        }
        return frame(L(), L(), list);
      case "parallax":
        k = inOutCubic(p);
        var edge = a * (1 - k);
        return frame(L({ offsetX: -0.35 * a * k, brightness: 1 - 0.5 * k }), L({ offsetX: edge }), null, edge > 0.001 && edge < a ? [band(edge - 0.06, edge, 0, 0.35, BLACK)] : []);
      case "whipPan":
        k = inOutExpo(p);
        var streak = 0.035 * Math.sin(Math.PI * p);
        return frame(L({ offsetX: -a * k, blur: streak }), L({ offsetX: a * (1 - k), blur: streak }));
      case "cardStack":
        k = outBack(p);
        return frame(L({ brightness: 1 - 0.35 * e, scale: 1 - 0.04 * e }), L({ offsetY: -1.1 * (1 - k), rotation: -8 * (1 - k), shadow: 0.7 * (1 - 0.6 * e), scale: 0.94 + 0.06 * e }));
      case "pageTurn":
        k = inOutCubic(p);
        var fold = a * (1 - k);
        return frame(L({ scaleX: Math.max(0.0001, 1 - k), pivotX: 0, brightness: 1 - 0.45 * k }), L({ brightness: 0.8 + 0.2 * k }), null,
          fold > 0.001 && fold < a ? [band(fold, Math.min(a, fold + 0.12), 0.45 * (1 - k), 0, BLACK)] : [], false);
      default: return frame(L(), L({ opacity: e }));
    }
  }

  /** Odak şeridi: kartın hâli (C# FocusPull.Of). */
  function focusOf(distance, blur) {
    var x = Math.min(Math.abs(distance), 1);
    return { scale: 1 - 0.14 * x, blur: Math.min(Math.abs(distance), 2) * blur, opacity: 1 - 0.5 * x, saturation: 1 - 0.45 * x };
  }

  function rgba(c, alpha) { return "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + clamp(alpha, 0, 1) + ")"; }

  /** Efekt karesini canvas'a çizer. drawFrom/drawTo(ctx) kareyi (0,0)-(w,h) alanına dönüşümsüz çizer. */
  function paint(ctx, w, h, f, drawFrom, drawTo) {
    function drawLayer(l, mask, draw) {
      if (l.opacity <= 0.001) { return; }
      ctx.save();
      if (mask) {
        ctx.beginPath();
        for (var m = 0; m < mask.length; m++) {
          var poly = mask[m];
          if (poly.length < 3) { continue; }
          ctx.moveTo(poly[0][0] * h, poly[0][1] * h);
          for (var q = 1; q < poly.length; q++) { ctx.lineTo(poly[q][0] * h, poly[q][1] * h); }
          ctx.closePath();
        }
        ctx.clip("nonzero");
      }
      var px = l.pivotX * h, py = l.pivotY * h;
      ctx.translate(l.offsetX * h, l.offsetY * h);
      ctx.translate(px, py);
      if (l.rotation) { ctx.rotate(l.rotation * Math.PI / 180); }
      ctx.scale(l.scale * l.scaleX, l.scale * l.scaleY);
      ctx.translate(-px, -py);
      var unit = Math.min(w, h);
      if (l.shadow > 0.001) {
        ctx.save();
        ctx.shadowColor = "rgba(0,0,0," + clamp(l.shadow, 0, 1) * 0.59 + ")";
        ctx.shadowBlur = unit * 0.06;
        ctx.shadowOffsetY = unit * 0.025;
        ctx.fillStyle = "#000";
        ctx.fillRect(0, 0, w, h);
        ctx.restore();
      }
      var filters = [];
      if (l.blur * unit > 0.3) { filters.push("blur(" + (l.blur * unit).toFixed(2) + "px)"); }
      if (Math.abs(l.saturation - 1) > 1e-4) { filters.push("saturate(" + l.saturation.toFixed(3) + ")"); }
      if (Math.abs(l.brightness - 1) > 1e-4) { filters.push("brightness(" + l.brightness.toFixed(3) + ")"); }
      if (filters.length) { ctx.filter = filters.join(" "); }
      ctx.globalAlpha = clamp(l.opacity, 0, 1);
      draw(ctx);
      ctx.restore();
    }

    if (f.toOnTop) { drawLayer(f.from, null, drawFrom); drawLayer(f.to, f.mask, drawTo); }
    else { drawLayer(f.to, f.mask, drawTo); drawLayer(f.from, null, drawFrom); }

    for (var i = 0; i < f.overlays.length; i++) {
      var o = f.overlays[i];
      ctx.save();
      if (o.type === "ring" && o.alpha > 0.001 && o.r > 0.0005) {
        ctx.strokeStyle = rgba(o.color, o.alpha);
        ctx.lineWidth = Math.max(1, o.width * h);
        ctx.beginPath(); ctx.arc(o.cx * h, o.cy * h, o.r * h, 0, Math.PI * 2); ctx.stroke();
      } else if (o.type === "solid" && o.alpha > 0.001) {
        ctx.fillStyle = rgba(o.color, o.alpha); ctx.fillRect(0, 0, w, h);
      } else if (o.type === "glow" && o.alpha > 0.001 && o.r > 0) {
        var g = ctx.createRadialGradient(o.cx * h, o.cy * h, 0, o.cx * h, o.cy * h, o.r * h);
        g.addColorStop(0, rgba(o.color, o.alpha)); g.addColorStop(1, rgba(o.color, 0));
        ctx.globalCompositeOperation = "screen"; ctx.fillStyle = g; ctx.fillRect(0, 0, w, h);
      } else if (o.type === "band" && Math.max(o.alpha0, o.alpha1) > 0.001 && Math.abs(o.x1 - o.x0) > 0.0001) {
        var lg = ctx.createLinearGradient(o.x0 * h, 0, o.x1 * h, 0);
        lg.addColorStop(0, rgba(o.color, o.alpha0)); lg.addColorStop(1, rgba(o.color, o.alpha1));
        ctx.fillStyle = lg; ctx.fillRect(Math.min(o.x0, o.x1) * h, 0, Math.abs(o.x1 - o.x0) * h, h);
      }
      ctx.restore();
    }
  }

  /** Karşılaştırma metni (C# EffectDescriber karşılığı). */
  function describe(f) {
    function n(v) { var r = Math.round(v * 10000) / 10000; if (r === 0) { r = 0; } return String(r); }
    function lay(name, l) {
      return name + ":" + [l.scale, l.scaleX, l.scaleY, l.offsetX, l.offsetY, l.rotation, l.pivotX, l.pivotY, l.opacity, l.blur, l.saturation, l.brightness, l.shadow].map(n).join(" ") + ";";
    }
    var s = lay("F", f.from) + lay("T", f.to) + (f.toOnTop ? "top;" : "under;");
    if (!f.mask) { s += "mask:none;"; }
    else {
      s += "mask:";
      for (var i = 0; i < f.mask.length; i++) {
        s += "[";
        for (var j = 0; j < f.mask[i].length; j++) { s += n(f.mask[i][j][0]) + "," + n(f.mask[i][j][1]) + " "; }
        s += "]";
      }
      s += ";";
    }
    for (var k = 0; k < f.overlays.length; k++) {
      var o = f.overlays[k];
      if (o.type === "ring") { s += "ring " + [o.cx, o.cy, o.r, o.width, o.alpha].map(n).join(" "); }
      else if (o.type === "solid") { s += "solid " + o.color[0] + " " + n(o.alpha); }
      else if (o.type === "glow") { s += "glow " + [o.cx, o.cy, o.r, o.alpha].map(n).join(" "); }
      else { s += "band " + [o.x0, o.x1, o.alpha0, o.alpha1].map(n).join(" "); }
      s += ";";
    }
    return s;
  }

  return { all: INFO, info: info, compute: compute, paint: paint, focusOf: focusOf, describe: describe, durationFactor: durationFactor };
})();
