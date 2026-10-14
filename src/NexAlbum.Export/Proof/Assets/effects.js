/*
 * NexAlbum pro efektleri — tarayıcı karşılığı (Karar 23).
 * Core/Effects/TransitionEffects.cs ile birebir aynı hesap: aynı efekt, ilerleme, oran, başlangıç noktası ve tohum aynı kareyi
 * verir (NexAlbum.Export.Tests içindeki eşdeğerlik testi denetler). Dış bağımlılık yok; internet gerekmez.
 * Alan: genişlik = en/boy oranı, yükseklik = 1.
 */
(function (root) {
  "use strict";

  var CIRCLE = 72;
  var WHITE = [255, 255, 255, 255], BLACK = [0, 0, 0, 255];
  var AMBER = [255, 170, 90, 255], ROSE = [255, 120, 140, 255], EMBER = [255, 140, 40, 255];

  // [kind, ad, grup, ipucu, başlangıç noktası kullanır mı]
  var ALL = [
    ["aperture", "Aperture", "Diyafram", "Tıklanan noktadan iris açılır, kenarda ince halka.", true],
    ["diaphragm", "Diyafram kanatları", "Diyafram", "Dönerek açılan altıgen objektif diyaframı.", false],
    ["doubleIris", "Çift iris", "Diyafram", "İki odaktan açılan daireler birleşir.", false],
    ["diamondReveal", "Elmas açılış", "Diyafram", "Ortadan büyüyen elmas.", false],
    ["iris", "Dairesel iris", "Diyafram", "Ortadan açılan daire.", false],
    ["rackFocus", "Netlik kaydırma", "Odak", "Eski kare bulanıklaşır, yenisi netleşerek gelir.", false],
    ["focusShift", "Odak kaydırma", "Odak", "Yumuşak bulanık geçiş.", false],
    ["tiltShift", "Tilt-shift", "Odak", "Ortadaki net bant genişleyerek açılır.", false],
    ["depthFocus", "Derinlikten", "Odak", "Yeni kare uzaktan, bulanık yaklaşır.", false],
    ["lightLeak", "Işık sızıntısı", "Işık ve film", "Sıcak turuncu-pembe parlama geçer.", false],
    ["filmBurn", "Film yanığı", "Işık ve film", "Bir noktadan yanarak açılan sıcak leke.", true],
    ["flash", "Flaş", "Işık ve film", "Beyaz patlama, ardından pozlama oturur.", false],
    ["projector", "Projektör", "Işık ve film", "Kısa titreme ve atlama ile kare değişir.", false],
    ["polaroidDevelop", "Polaroid banyosu", "Işık ve film", "Fotoğraf beyazdan renge yavaşça belirir.", false],
    ["crossfade", "Çapraz geçiş", "Klasik", "Yumuşak karışım.", false],
    ["dipToBlack", "Siyaha geçiş", "Klasik", "Siyaha kararıp açılır.", false],
    ["dipToWhite", "Beyaza geçiş", "Klasik", "Beyaza açılıp yeni kare gelir.", false],
    ["cut", "Kesme", "Klasik", "Doğrudan geçiş.", false],
    ["curtain", "Perde", "Maske", "Ortadan iki yana açılır.", false],
    ["split", "Yarık", "Maske", "Yatay yarıktan yeni kare yaklaşarak çıkar.", false],
    ["blinds", "Panjur", "Maske", "Yatay şeritler sırayla açılır.", false],
    ["diagonalWipe", "Çapraz silme", "Maske", "Eğik çizgiyle silinir.", false],
    ["mosaic", "Mozaik", "Maske", "Kareler sırayla belirir.", false],
    ["honeycomb", "Petek", "Maske", "Altıgen hücrelerle açılır.", false],
    ["inkBlot", "Mürekkep lekesi", "Maske", "Organik lekeler yayılarak açar.", true],
    ["parallax", "Paralaks", "Hareket", "Katmanlar farklı hızda kayar.", false],
    ["whipPan", "Hızlı kaydırma", "Hareket", "Kamera hızla döner; hareket bulanıklığı.", false],
    ["pushLeft", "Sola itme", "Hareket", "Yeni kare sağdan iter.", false],
    ["pushRight", "Sağa itme", "Hareket", "Yeni kare soldan iter.", false],
    ["pushUp", "Yukarı itme", "Hareket", "Yeni kare alttan iter.", false],
    ["zoomThrough", "Yakınlaşarak", "Hareket", "İçinden geçer gibi yakınlaşır.", false],
    ["cardStack", "Kart yığını", "Albüm", "Yeni fotoğraf hafif dönerek üste düşer.", false],
    ["pageTurn", "Sayfa çevirme", "Albüm", "Sayfa sırt çevresinde döner.", false]
  ].map(function (e) { return { kind: e[0], name: e[1], group: e[2], hint: e[3], usesOrigin: e[4] }; });

  function info(kind) {
    for (var i = 0; i < ALL.length; i++) { if (ALL[i].kind === kind) { return ALL[i]; } }
    return ALL[0];
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

  // ---- yumuşatmalar (C# ile aynı)
  function clamp(v, a, b) { return Math.min(Math.max(v, a), b); }
  function smooth(p) { return p * p * (3 - 2 * p); }
  function smoothStep(a, b, p) { return smooth(clamp((p - a) / (b - a), 0, 1)); }
  function outCubic(p) { return 1 - Math.pow(1 - p, 3); }
  function inOutCubic(p) { return p < 0.5 ? 4 * p * p * p : 1 - Math.pow(-2 * p + 2, 3) / 2; }
  function irisEase(p) { return p < 0.5 ? 8 * Math.pow(p, 4) : 1 - Math.pow(-2 * p + 2, 4) / 2; }
  function inOutExpo(p) { return p <= 0 ? 0 : p >= 1 ? 1 : p < 0.5 ? Math.pow(2, 20 * p - 10) / 2 : (2 - Math.pow(2, -20 * p + 10)) / 2; }
  function outBack(p) { var c1 = 1.2, c3 = c1 + 1; return 1 + c3 * Math.pow(p - 1, 3) + c1 * Math.pow(p - 1, 2); }
  function coverRadius(x, y, a) { return Math.sqrt(Math.pow(Math.max(x, a - x), 2) + Math.pow(Math.max(y, 1 - y), 2)); }

  function mulberry32(seed) {
    var state = seed >>> 0;
    return function () {
      state = (state + 0x6D2B79F5) >>> 0;
      var t = state;
      t = Math.imul(t ^ (t >>> 15), t | 1) >>> 0;
      t = (t ^ ((t + (Math.imul(t ^ (t >>> 7), t | 61) >>> 0)) >>> 0)) >>> 0;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

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
    for (var i = 0; i < CIRCLE; i++) { var t = 2 * Math.PI * i / CIRCLE; pts.push([cx + r * Math.cos(t), cy + r * Math.sin(t)]); }
    return pts;
  }

  function regularPolygon(cx, cy, r, sides, rot) {
    var pts = [];
    for (var i = 0; i < sides; i++) { var t = 2 * Math.PI * i / sides + rot * Math.PI / 180; pts.push([cx + r * Math.cos(t), cy + r * Math.sin(t)]); }
    return pts;
  }

  function rect(x, y, w, h) { return [[x, y], [x + w, y], [x + w, y + h], [x, y + h]]; }

  function blob(cx, cy, radius, roughness, rnd) {
    var f1 = 3 + Math.floor(rnd() * 3), f2 = 7 + Math.floor(rnd() * 4), f3 = 13 + Math.floor(rnd() * 5);
    var p1 = rnd() * Math.PI * 2, p2 = rnd() * Math.PI * 2, p3 = rnd() * Math.PI * 2;
    var pts = [];
    for (var i = 0; i < CIRCLE; i++) {
      var t = 2 * Math.PI * i / CIRCLE;
      var wobble = 0.6 * Math.sin(f1 * t + p1) + 0.3 * Math.sin(f2 * t + p2) + 0.1 * Math.sin(f3 * t + p3);
      var rr = radius * (1 + roughness * wobble);
      pts.push([cx + rr * Math.cos(t), cy + rr * Math.sin(t)]);
    }
    return pts;
  }

  function shuffle(n, rnd) {
    var order = [];
    for (var i = 0; i < n; i++) { order.push(i); }
    for (var j = n - 1; j > 0; j--) { var k = Math.floor(rnd() * (j + 1)); var tmp = order[j]; order[j] = order[k]; order[k] = tmp; }
    return order;
  }

  function ring(cx, cy, r, w, alpha, color) { return { type: "ring", cx: cx, cy: cy, r: r, width: w, alpha: alpha, color: color }; }
  function solid(color, alpha) { return { type: "solid", color: color, alpha: alpha }; }
  function glow(cx, cy, r, color, alpha) { return { type: "glow", cx: cx, cy: cy, r: r, color: color, alpha: alpha }; }
  function band(x0, x1, a0, a1, color) { return { type: "band", x0: x0, x1: x1, alpha0: a0, alpha1: a1, color: color }; }

  /**
   * Geçişin p (0–1) anındaki hâli. origin: [x, y] (efekt alanında) ya da null; seed: 32 bit tohum.
   */
  function compute(kind, p, aspect, origin, seed) {
    if (!(aspect > 0) || !isFinite(aspect)) { throw new Error("aspect"); }
    p = clamp(isFinite(p) ? p : 0, 0, 1);
    var a = aspect, cx = a / 2;
    var S = function (o) { return layer(a, o); };
    var r = mulberry32(seed === undefined ? 1 : seed);
    var o = origin || (info(kind).usesOrigin ? [a * (0.25 + 0.5 * r()), 0.3 + 0.4 * r()] : [cx, 0.5]);
    var ox = o[0], oy = o[1];
    var cover = coverRadius(ox, oy, a);
    var e = smooth(p), k, radius, half;

    switch (kind) {
      case "cut":
        return frame(S({ opacity: p < 0.5 ? 1 : 0 }), S({ opacity: p < 0.5 ? 0 : 1 }));
      case "dipToBlack":
      case "dipToWhite":
        return frame(S({ opacity: p < 0.5 ? 1 : 0 }), S({ opacity: p < 0.5 ? 0 : 1 }), null,
          [solid(kind === "dipToBlack" ? BLACK : WHITE, 1 - Math.abs(p * 2 - 1))]);
      case "focusShift":
        return frame(S({ blur: 0.03 * e }), S({ opacity: e, blur: 0.03 * (1 - e) }));
      case "iris":
        return frame(S(), S(), [circle(cx, 0.5, e * coverRadius(cx, 0.5, a))]);
      case "pushLeft":
      case "pushRight":
      case "pushUp": {
        var dx = kind === "pushLeft" ? -a : kind === "pushRight" ? a : 0;
        var dy = kind === "pushUp" ? -1 : 0;
        return frame(S({ offsetX: dx * e, offsetY: dy * e }), S({ offsetX: -dx * (1 - e), offsetY: -dy * (1 - e) }));
      }
      case "zoomThrough":
        return frame(S({ scale: 1 + e * 0.5, blur: 0.02 * e }), S({ scale: 0.85 + e * 0.15, opacity: e, blur: 0.02 * (1 - e) }));
      case "aperture": {
        k = irisEase(p);
        radius = k * cover;
        var ringAlpha = p < 0.7 ? 1 : 1 - (p - 0.7) / 0.3;
        return frame(S(), S({ scale: 1.18 - 0.18 * outCubic(Math.min(1, p / 0.8)), pivotX: ox, pivotY: oy }),
          [circle(ox, oy, radius)], [ring(ox, oy, radius, 0.0022, ringAlpha * (radius > 0.002 ? 1 : 0), WHITE)]);
      }
      case "diaphragm": {
        k = irisEase(p);
        radius = k * cover * 1.16;
        var turn = (1 - k) * 60;
        return frame(S({ brightness: 1 - 0.3 * k }), S({ scale: 1.08 - 0.08 * k }),
          [regularPolygon(cx, 0.5, radius, 6, turn)], [ring(cx, 0.5, radius * 0.87, 0.0016, (1 - k) * 0.8, WHITE)]);
      }
      case "doubleIris": {
        k = irisEase(p);
        var c1x = a * 0.3, c1y = 0.4, c2x = a * 0.7, c2y = 0.6;
        radius = k * Math.max(coverRadius(c1x, c1y, a), coverRadius(c2x, c2y, a)) * 0.9;
        return frame(S(), S(), [circle(c1x, c1y, radius), circle(c2x, c2y, radius)]);
      }
      case "diamondReveal": {
        var d = irisEase(p) * (cx + 0.5) * 1.02;
        return frame(S(), S(), [[[cx - d, 0.5], [cx, 0.5 - d], [cx + d, 0.5], [cx, 0.5 + d]]]);
      }
      case "rackFocus":
        return frame(S({ blur: 0.05 * e, scale: 1 + 0.04 * e }), S({ opacity: smoothStep(0.3, 0.8, p), blur: 0.05 * (1 - e), scale: 1.04 - 0.04 * e }));
      case "tiltShift":
        half = 0.5 * e;
        return frame(S({ blur: 0.03 * e }), S({ blur: 0.02 * (1 - e) }), [rect(-1, 0.5 - half, a + 2, half * 2)]);
      case "depthFocus":
        return frame(S({ scale: 1 + 0.15 * e, blur: 0.03 * e }),
          S({ scale: 0.6 + 0.4 * outCubic(p), blur: 0.06 * (1 - e), opacity: smoothStep(0, 0.6, p), shadow: 0.6 * (1 - e) }));
      case "lightLeak": {
        var bell = Math.sin(Math.PI * p);
        return frame(S({ brightness: 1 + 0.25 * bell }), S({ opacity: smoothStep(0.35, 0.75, p), brightness: 1 + 0.25 * bell }), null,
          [glow(-0.3 * a + 1.6 * a * p, 0.3, 0.9, AMBER, bell * 0.85), glow(1.2 * a - 1.4 * a * p, 0.8, 0.7, ROSE, bell * 0.6)]);
      }
      case "filmBurn":
        radius = e * cover * 1.25;
        return frame(S({ brightness: 1 + 0.6 * e }), S(), [blob(ox, oy, radius, 0.18, r)],
          [glow(ox, oy, radius * 1.3 + 0.1, EMBER, Math.sin(Math.PI * p) * 0.9)]);
      case "flash": {
        var fl = p < 0.5 ? Math.pow(p / 0.5, 3) : 1 - Math.pow((p - 0.5) / 0.5, 0.6);
        return frame(S({ opacity: p < 0.5 ? 1 : 0 }), S({ opacity: p < 0.5 ? 0 : 1, brightness: 1 + 0.5 * (1 - e) }), null, [solid(WHITE, fl)]);
      }
      case "projector": {
        var step = Math.floor(p * 12);
        var showNew = p >= 0.6 || (p >= 0.25 && step % 2 === 1);
        var jitter = 0.012 * Math.sin(p * 50) * (1 - e);
        var flicker = (p > 0.15 && p < 0.75) ? 0.25 * (step % 2) : 0;
        return frame(S({ opacity: showNew ? 0 : 1, offsetY: jitter, brightness: 1 - flicker }),
          S({ opacity: showNew ? 1 : 0, offsetY: -jitter, brightness: 1 - flicker }));
      }
      case "polaroidDevelop":
        return frame(S(), S({ opacity: smoothStep(0, 0.3, p), saturation: e * e, brightness: 1 + 1.2 * (1 - e), scale: 0.96 + 0.04 * e, shadow: 0.4 * (1 - e) }));
      case "crossfade":
        return frame(S(), S({ opacity: e }));
      case "curtain":
        half = inOutCubic(p) * cx;
        return frame(S({ brightness: 1 - 0.35 * e }), S(), [rect(cx - half, -1, half * 2, 3)],
          half > 0.001 ? [band(cx - half - 0.04, cx - half, 0, 0.45 * (1 - e), BLACK), band(cx + half, cx + half + 0.04, 0.45 * (1 - e), 0, BLACK)] : []);
      case "split":
        half = inOutCubic(p) * 0.5;
        return frame(S({ brightness: 1 - 0.4 * e }), S({ scale: 1.15 - 0.15 * e }), [rect(-1, 0.5 - half, a + 2, half * 2)]);
      case "blinds": {
        var strips = 8, rects = [];
        for (var i = 0; i < strips; i++) {
          var local = inOutCubic(clamp((p - i * 0.05) / 0.65, 0, 1));
          if (local > 0) { rects.push(rect(-1, i / strips, a + 2, local / strips)); }
        }
        return frame(S(), S(), rects);
      }
      case "diagonalWipe": {
        var c = -0.5 + inOutCubic(p) * (a + 1);
        return frame(S(), S(), [[[-1, -1], [c + 0.5, -1], [c - 0.5, 2], [-1, 2]]]);
      }
      case "mosaic": {
        var rows = 6, cols = Math.ceil(a * rows), size = 1 / rows;
        var order = shuffle(cols * rows, r), cells = [];
        for (var m = 0; m < order.length; m++) {
          var delay = order[m] / order.length * 0.6;
          var lm = outCubic(clamp((p - delay) / 0.4, 0, 1));
          if (lm <= 0) { continue; }
          var col = m % cols, row = Math.floor(m / cols), s = size * lm * 1.02;
          cells.push(rect(col * size + (size - s) / 2, row * size + (size - s) / 2, s, s));
        }
        return frame(S(), S(), cells);
      }
      case "honeycomb": {
        var R = 0.09, w = Math.sqrt(3) * R, hex = [], maxD = coverRadius(cx, 0.5, a);
        for (var hr = -1; hr * 1.5 * R < 1 + R; hr++) {
          for (var hc = -1; hc * w < a + w; hc++) {
            var hx = hc * w + (hr % 2 === 0 ? 0 : w / 2), hy = hr * 1.5 * R;
            var dist = Math.sqrt((hx - cx) * (hx - cx) + (hy - 0.5) * (hy - 0.5)) / maxD;
            var lh = outCubic(clamp((p - dist * 0.55) / 0.45, 0, 1));
            if (lh > 0) { hex.push(regularPolygon(hx, hy, R * 1.04 * lh, 6, 30)); }
          }
        }
        return frame(S(), S(), hex);
      }
      case "inkBlot": {
        var blobs = [], main = outCubic(p) * cover * 1.4;
        if (main > 0) { blobs.push(blob(ox, oy, main, 0.25, r)); }
        for (var b = 0; b < 3; b++) {
          var bx = a * (0.1 + 0.8 * r()), by = 0.1 + 0.8 * r();
          var lb = outCubic(clamp((p - 0.1 - b * 0.12) / 0.6, 0, 1));
          var rb = lb * coverRadius(bx, by, a) * 0.8;
          if (rb > 0) { blobs.push(blob(bx, by, rb, 0.3, r)); }
        }
        return frame(S(), S(), blobs);
      }
      case "parallax": {
        k = inOutCubic(p);
        var edge = a * (1 - k);
        return frame(S({ offsetX: -0.35 * a * k, brightness: 1 - 0.5 * k }), S({ offsetX: edge }), null,
          edge > 0.001 && edge < a ? [band(edge - 0.06, edge, 0, 0.35, BLACK)] : []);
      }
      case "whipPan": {
        k = inOutExpo(p);
        var streak = 0.035 * Math.sin(Math.PI * p);
        return frame(S({ offsetX: -a * k, blur: streak }), S({ offsetX: a * (1 - k), blur: streak }));
      }
      case "cardStack":
        k = outBack(p);
        return frame(S({ brightness: 1 - 0.35 * e, scale: 1 - 0.04 * e }),
          S({ offsetY: -1.1 * (1 - k), rotation: -8 * (1 - k), shadow: 0.7 * (1 - 0.6 * e), scale: 0.94 + 0.06 * e }));
      case "pageTurn": {
        k = inOutCubic(p);
        var fold = a * (1 - k);
        return frame(S({ scaleX: Math.max(0.0001, 1 - k), pivotX: 0, brightness: 1 - 0.45 * k }), S({ brightness: 0.8 + 0.2 * k }), null,
          fold > 0.001 && fold < a ? [band(fold, Math.min(a, fold + 0.12), 0.45 * (1 - k), 0, BLACK)] : [], false);
      }
      default:
        return frame(S(), S({ opacity: e }));
    }
  }

  // ---- C# EffectDescriber ile aynı metin (eşdeğerlik testi)
  function n4(v) {
    var r = (v < 0 ? -1 : 1) * Math.round(Math.abs(v) * 10000) / 10000;
    if (r === 0) { return "0"; }
    var s = r.toFixed(4);
    s = s.replace(/0+$/, "").replace(/\.$/, "");
    return s === "-0" ? "0" : s;
  }

  function describeLayer(name, l) {
    return name + ":" + [l.scale, l.scaleX, l.scaleY, l.offsetX, l.offsetY, l.rotation, l.pivotX, l.pivotY, l.opacity, l.blur, l.saturation, l.brightness, l.shadow].map(n4).join(" ") + ";";
  }

  function describe(f) {
    var s = describeLayer("F", f.from) + describeLayer("T", f.to) + (f.toOnTop ? "top;" : "under;");
    if (!f.mask) {
      s += "mask:none;";
    } else {
      s += "mask:";
      f.mask.forEach(function (poly) { s += "["; poly.forEach(function (pt) { s += n4(pt[0]) + "," + n4(pt[1]) + " "; }); s += "]"; });
      s += ";";
    }
    f.overlays.forEach(function (o) {
      if (o.type === "ring") { s += "ring " + n4(o.cx) + " " + n4(o.cy) + " " + n4(o.r) + " " + n4(o.width) + " " + n4(o.alpha); }
      else if (o.type === "solid") { s += "solid " + o.color[0] + " " + n4(o.alpha); }
      else if (o.type === "glow") { s += "glow " + n4(o.cx) + " " + n4(o.cy) + " " + n4(o.r) + " " + n4(o.alpha); }
      else if (o.type === "band") { s += "band " + n4(o.x0) + " " + n4(o.x1) + " " + n4(o.alpha0) + " " + n4(o.alpha1); }
      s += ";";
    });
    return s;
  }

  // ---- odak şeridi (FocusPull.cs)
  function focusOf(distance, blur) {
    var a = Math.min(Math.abs(distance), 1);
    return { scale: 1 - 0.14 * a, blur: Math.min(Math.abs(distance), 2) * blur, opacity: 1 - 0.5 * a, saturation: 1 - 0.45 * a };
  }

  // ---- tuval çizici (Rendering/Effects/EffectPainter.cs karşılığı)
  function rgba(c, alpha) { return "rgba(" + c[0] + "," + c[1] + "," + c[2] + "," + clamp(alpha, 0, 1) * (c[3] / 255) + ")"; }

  function drawLayer(ctx, w, h, l, mask, draw) {
    if (!(l.opacity > 0.001)) { return; }
    ctx.save();
    if (mask) {
      ctx.beginPath();
      var any = false;
      mask.forEach(function (poly) {
        if (poly.length < 3) { return; }
        any = true;
        ctx.moveTo(poly[0][0] * h, poly[0][1] * h);
        for (var i = 1; i < poly.length; i++) { ctx.lineTo(poly[i][0] * h, poly[i][1] * h); }
        ctx.closePath();
      });
      if (!any) { ctx.restore(); return; }
      ctx.clip("nonzero");
    }
    var px = l.pivotX * h, py = l.pivotY * h;
    ctx.translate(l.offsetX * h, l.offsetY * h);
    if (Math.abs(l.rotation) > 1e-6) { ctx.translate(px, py); ctx.rotate(l.rotation * Math.PI / 180); ctx.translate(-px, -py); }
    var sx = l.scale * l.scaleX, sy = l.scale * l.scaleY;
    if (Math.abs(sx - 1) > 1e-6 || Math.abs(sy - 1) > 1e-6) { ctx.translate(px, py); ctx.scale(sx, sy); ctx.translate(-px, -py); }
    var unit = Math.min(w, h);
    if (l.shadow > 0.001) {
      ctx.save();
      ctx.filter = "blur(" + (unit * 0.03).toFixed(2) + "px)";
      ctx.fillStyle = "rgba(0,0,0," + (clamp(l.shadow, 0, 1) * 150 / 255).toFixed(4) + ")";
      ctx.fillRect(unit * 0.01, unit * 0.025, w, h);
      ctx.restore();
    }
    var filters = [];
    var sigma = l.blur * unit;
    if (sigma > 0.3) { filters.push("blur(" + sigma.toFixed(2) + "px)"); }
    if (Math.abs(l.saturation - 1) > 1e-4) { filters.push("saturate(" + l.saturation.toFixed(4) + ")"); }
    if (Math.abs(l.brightness - 1) > 1e-4) { filters.push("brightness(" + l.brightness.toFixed(4) + ")"); }
    ctx.filter = filters.length ? filters.join(" ") : "none";
    ctx.globalAlpha = clamp(l.opacity, 0, 1);
    draw(ctx, w, h);
    ctx.restore();
  }

  function drawOverlay(ctx, w, h, o) {
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
      var x0 = o.x0 * h, x1 = o.x1 * h;
      var lg = ctx.createLinearGradient(x0, 0, x1, 0);
      lg.addColorStop(0, rgba(o.color, o.alpha0)); lg.addColorStop(1, rgba(o.color, o.alpha1));
      ctx.fillStyle = lg; ctx.fillRect(Math.min(x0, x1), 0, Math.abs(x1 - x0), h);
    }
    ctx.restore();
  }

  /** Kareyi tuvale çizer; drawFrom/drawTo (ctx, w, h) görüntüyü (0,0)–(w,h) alanına dönüşümsüz çizer. */
  function paint(ctx, w, h, f, drawFrom, drawTo) {
    if (f.toOnTop) { drawLayer(ctx, w, h, f.from, null, drawFrom); drawLayer(ctx, w, h, f.to, f.mask, drawTo); }
    else { drawLayer(ctx, w, h, f.to, f.mask, drawTo); drawLayer(ctx, w, h, f.from, null, drawFrom); }
    f.overlays.forEach(function (o) { drawOverlay(ctx, w, h, o); });
  }

  root.NexEffects = {
    all: ALL, info: info, durationFactor: durationFactor, compute: compute, describe: describe, focusOf: focusOf, paint: paint,
    coverRadius: coverRadius, irisEase: irisEase, outCubic: outCubic, inOutCubic: inOutCubic
  };
})(typeof window !== "undefined" ? window : globalThis);
