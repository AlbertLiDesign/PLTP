// A small WebGL2 viewer for the extracted surface.
//
// Written out rather than pulled from a CDN on purpose: the point of this app is
// that a fresh clone runs offline with nothing but the .NET SDK, and a viewer
// that needs the network to draw anything would undo that. It only has to do one
// job - show one static triangle soup well - so it is a few hundred lines rather
// than a library.

const VERT = `#version 300 es
precision highp float;
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
uniform mat4 uProj;
uniform mat4 uView;      // world -> eye, including the up-axis rotation
out vec3 vNormalEye;
out vec3 vPosEye;
out vec3 vModel;         // untransformed, so the clip plane is in model units
void main() {
  vec4 eye = uView * vec4(aPos, 1.0);
  vPosEye = eye.xyz;
  vNormalEye = mat3(uView) * aNormal;
  vModel = aPos;
  gl_Position = uProj * eye;
}`;

const FRAG = `#version 300 es
precision highp float;
in vec3 vNormalEye;
in vec3 vPosEye;
in vec3 vModel;
uniform vec3 uColor;
uniform vec3 uBackColor;
uniform bool uFlat;
uniform bool uClipOn;
uniform vec4 uClip;       // xyz normal, w offset: discard where dot(p, n) > w
uniform float uOpacity;
out vec4 frag;

void main() {
  if (uClipOn && dot(vModel, uClip.xyz) > uClip.w) discard;

  // Flat shading from screen-space derivatives: the extracted surface is a
  // marching-cubes-style patchwork, and seeing the actual facets is often what
  // the user is checking. Smooth uses the accumulated vertex normals instead.
  vec3 n = uFlat ? normalize(cross(dFdx(vPosEye), dFdy(vPosEye))) : normalize(vNormalEye);
  vec3 base = uColor;
  if (!gl_FrontFacing) { n = -n; base = uBackColor; }

  vec3 v = normalize(-vPosEye);

  // Three lights fixed in eye space, so the model is lit from the same side
  // however it is turned. A headlight alone flattens the shape out.
  vec3 key  = normalize(vec3( 0.45,  0.65,  0.80));
  vec3 fill = normalize(vec3(-0.70,  0.15,  0.45));
  vec3 rim  = normalize(vec3( 0.10, -0.90,  0.25));

  float d = max(dot(n, key), 0.0) * 0.85
          + max(dot(n, fill), 0.0) * 0.32
          + max(dot(n, rim), 0.0) * 0.18;

  // Hemispheric ambient, sky above and a cooler bounce below.
  float up = n.y * 0.5 + 0.5;
  vec3 ambient = mix(vec3(0.16, 0.17, 0.21), vec3(0.42, 0.45, 0.52), up);

  vec3 h = normalize(key + v);
  float spec = pow(max(dot(n, h), 0.0), 42.0) * 0.30;

  float fres = pow(1.0 - max(dot(n, v), 0.0), 3.0) * 0.22;

  vec3 c = base * (ambient + d) + vec3(spec) + vec3(fres) * vec3(0.55, 0.70, 1.0);
  frag = vec4(pow(clamp(c, 0.0, 1.0), vec3(1.0 / 2.2)), uOpacity);
}`;

const LINE_VERT = `#version 300 es
precision highp float;
layout(location = 0) in vec3 aPos;
uniform mat4 uProj;
uniform mat4 uView;
uniform float uDepthNudge;
void main() {
  vec4 p = uProj * uView * vec4(aPos, 1.0);
  // Pull the wire a hair toward the camera so it is not z-fought by the surface
  // it traces.
  p.z -= uDepthNudge * p.w;
  gl_Position = p;
}`;

const LINE_FRAG = `#version 300 es
precision highp float;
uniform vec4 uColor;
out vec4 frag;
void main() { frag = uColor; }`;

const BG_VERT = `#version 300 es
precision highp float;
const vec2 P[3] = vec2[3](vec2(-1.0, -1.0), vec2(3.0, -1.0), vec2(-1.0, 3.0));
out vec2 vUv;
void main() {
  vec2 p = P[gl_VertexID];
  vUv = p * 0.5 + 0.5;
  gl_Position = vec4(p, 0.0, 1.0);
}`;

const BG_FRAG = `#version 300 es
precision highp float;
in vec2 vUv;
uniform vec3 uTop;
uniform vec3 uBottom;
out vec4 frag;
void main() {
  vec3 c = mix(uBottom, uTop, smoothstep(0.0, 1.0, vUv.y));
  // A touch of vignette keeps the model reading against a flat panel.
  float r = distance(vUv, vec2(0.5)) * 1.25;
  c *= 1.0 - r * r * 0.35;
  frag = vec4(c, 1.0);
}`;

/* ---------------------------------------------------------------- mat4 ---- */

const HALF_PI = Math.PI / 2;

function mat4() { return new Float32Array(16); }

function perspective(out, fovy, aspect, near, far) {
  const f = 1 / Math.tan(fovy / 2);
  out.fill(0);
  out[0] = f / aspect; out[5] = f; out[11] = -1;
  out[10] = (far + near) / (near - far);
  out[14] = (2 * far * near) / (near - far);
  return out;
}

function lookAt(out, eye, target, up) {
  let zx = eye[0] - target[0], zy = eye[1] - target[1], zz = eye[2] - target[2];
  let n = Math.hypot(zx, zy, zz) || 1; zx /= n; zy /= n; zz /= n;

  let xx = up[1] * zz - up[2] * zy, xy = up[2] * zx - up[0] * zz, xz = up[0] * zy - up[1] * zx;
  n = Math.hypot(xx, xy, xz) || 1; xx /= n; xy /= n; xz /= n;

  const yx = zy * xz - zz * xy, yy = zz * xx - zx * xz, yz = zx * xy - zy * xx;

  out[0] = xx; out[4] = xy; out[8] = xz; out[12] = -(xx * eye[0] + xy * eye[1] + xz * eye[2]);
  out[1] = yx; out[5] = yy; out[9] = yz; out[13] = -(yx * eye[0] + yy * eye[1] + yz * eye[2]);
  out[2] = zx; out[6] = zy; out[10] = zz; out[14] = -(zx * eye[0] + zy * eye[1] + zz * eye[2]);
  out[3] = 0; out[7] = 0; out[11] = 0; out[15] = 1;
  return out;
}

/* --------------------------------------------------------------- viewer ---- */

export function createViewer(canvas, gizmoSvg) {
  const gl = canvas.getContext('webgl2', {
    antialias: true,
    // The screenshot button reads the canvas back after a draw, and without this
    // the browser is free to have discarded it by then.
    preserveDrawingBuffer: true,
  });
  if (!gl) throw new Error('This browser has no WebGL2. Try a current Chrome, Edge, Firefox or Safari.');

  const meshProg = program(gl, VERT, FRAG);
  const lineProg = program(gl, LINE_VERT, LINE_FRAG);
  const bgProg = program(gl, BG_VERT, BG_FRAG);

  const emptyVao = gl.createVertexArray();

  const state = {
    vao: null, posBuf: null, normBuf: null, idxBuf: null,
    edgeBuf: null, edgeCount: 0, edgeTried: false,
    boxBuf: null,
    triangles: 0, vertices: 0,
    positions: null, indices: null,
    min: [0, 0, 0], max: [0, 0, 0], centre: [0, 0, 0], radius: 1,

    flat: false,
    wireframe: false,
    showBox: false,
    zUp: true,
    color: [0.78, 0.80, 0.85],
    backColor: [0.85, 0.42, 0.32],
    clipOn: false, clipAxis: 0, clipT: 1, clipFlip: false,

    theta: -0.9, phi: 1.05, distance: 4, target: [0, 0, 0],
  };

  const proj = mat4(), view = mat4();

  let frame = null;

  function invalidate() {
    if (frame !== null) return;
    frame = requestAnimationFrame(() => { frame = null; render(); });
  }

  /* ------------------------------------------------------------ geometry -- */

  function setMesh(buffer) {
    const dv = new DataView(buffer);
    const magic = String.fromCharCode(...new Uint8Array(buffer, 0, 8));
    if (magic !== 'PLTPMSH1') throw new Error('Unrecognised mesh payload.');

    const vertexCount = dv.getUint32(8, true);
    const triangleCount = dv.getUint32(12, true);
    const positions = new Float32Array(buffer, 16, vertexCount * 3);
    const indices = new Uint32Array(buffer, 16 + vertexCount * 12, triangleCount * 3);

    disposeGeometry();

    state.vertices = vertexCount;
    state.triangles = triangleCount;
    state.positions = positions;
    state.indices = indices;
    state.edgeTried = false;
    state.edgeCount = 0;

    const normals = smoothNormals(positions, indices);

    state.vao = gl.createVertexArray();
    gl.bindVertexArray(state.vao);

    state.posBuf = buffer_(gl, gl.ARRAY_BUFFER, positions);
    gl.enableVertexAttribArray(0);
    gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 0, 0);

    state.normBuf = buffer_(gl, gl.ARRAY_BUFFER, normals);
    gl.enableVertexAttribArray(1);
    gl.vertexAttribPointer(1, 3, gl.FLOAT, false, 0, 0);

    state.idxBuf = gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, state.idxBuf);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, indices, gl.STATIC_DRAW);

    gl.bindVertexArray(null);

    measure(positions, vertexCount);
    buildBox();
    if (state.wireframe) ensureEdges();
    fit();
  }

  function measure(positions, vertexCount) {
    const min = [Infinity, Infinity, Infinity];
    const max = [-Infinity, -Infinity, -Infinity];
    for (let i = 0; i < vertexCount; i++) {
      for (let k = 0; k < 3; k++) {
        const v = positions[i * 3 + k];
        if (v < min[k]) min[k] = v;
        if (v > max[k]) max[k] = v;
      }
    }
    if (!isFinite(min[0])) { min[0] = min[1] = min[2] = 0; max[0] = max[1] = max[2] = 1; }
    state.min = min;
    state.max = max;
    state.centre = [(min[0] + max[0]) / 2, (min[1] + max[1]) / 2, (min[2] + max[2]) / 2];
    state.radius = Math.max(1e-6, Math.hypot(max[0] - min[0], max[1] - min[1], max[2] - min[2]) / 2);
  }

  // Area-weighted vertex normals. The mesh is welded server-side, so summing the
  // cross products of the faces around a vertex is enough - the shared vertices
  // really are shared, and the weight falls out of the unnormalised cross product.
  function smoothNormals(positions, indices) {
    const normals = new Float32Array(positions.length);
    for (let i = 0; i < indices.length; i += 3) {
      const a = indices[i] * 3, b = indices[i + 1] * 3, c = indices[i + 2] * 3;
      const ux = positions[b] - positions[a];
      const uy = positions[b + 1] - positions[a + 1];
      const uz = positions[b + 2] - positions[a + 2];
      const vx = positions[c] - positions[a];
      const vy = positions[c + 1] - positions[a + 1];
      const vz = positions[c + 2] - positions[a + 2];
      const nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
      normals[a] += nx; normals[a + 1] += ny; normals[a + 2] += nz;
      normals[b] += nx; normals[b + 1] += ny; normals[b + 2] += nz;
      normals[c] += nx; normals[c + 1] += ny; normals[c + 2] += nz;
    }
    for (let i = 0; i < normals.length; i += 3) {
      const n = Math.hypot(normals[i], normals[i + 1], normals[i + 2]);
      if (n > 0) { normals[i] /= n; normals[i + 1] /= n; normals[i + 2] /= n; }
    }
    return normals;
  }

  const EDGE_LIMIT = 1_500_000;   // triangles; past this the Set costs more than the wire is worth

  function ensureEdges() {
    if (state.edgeBuf || state.edgeTried || !state.indices) return;
    state.edgeTried = true;
    if (state.triangles > EDGE_LIMIT) return;

    const seen = new Set();
    const edges = [];
    const idx = state.indices;
    for (let i = 0; i < idx.length; i += 3) {
      for (let e = 0; e < 3; e++) {
        const a = idx[i + e], b = idx[i + (e + 1) % 3];
        const lo = a < b ? a : b, hi = a < b ? b : a;
        const key = lo * 4294967296 + hi;
        if (seen.has(key)) continue;
        seen.add(key);
        edges.push(lo, hi);
      }
    }
    state.edgeCount = edges.length;
    state.edgeBuf = gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, state.edgeBuf);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint32Array(edges), gl.STATIC_DRAW);
  }

  function buildBox() {
    const [x0, y0, z0] = state.min, [x1, y1, z1] = state.max;
    const c = [
      x0, y0, z0, x1, y0, z0, x1, y1, z0, x0, y1, z0,
      x0, y0, z1, x1, y0, z1, x1, y1, z1, x0, y1, z1,
    ];
    const lines = [0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7];
    const flat = new Float32Array(lines.length * 3);
    lines.forEach((v, i) => { flat[i * 3] = c[v * 3]; flat[i * 3 + 1] = c[v * 3 + 1]; flat[i * 3 + 2] = c[v * 3 + 2]; });
    if (state.boxBuf) gl.deleteBuffer(state.boxBuf);
    state.boxBuf = buffer_(gl, gl.ARRAY_BUFFER, flat);
  }

  function disposeGeometry() {
    for (const b of [state.posBuf, state.normBuf, state.idxBuf, state.edgeBuf, state.boxBuf])
      if (b) gl.deleteBuffer(b);
    if (state.vao) gl.deleteVertexArray(state.vao);
    state.vao = state.posBuf = state.normBuf = state.idxBuf = state.edgeBuf = state.boxBuf = null;
    state.triangles = state.vertices = state.edgeCount = 0;
    state.positions = state.indices = null;
  }

  function clear() {
    disposeGeometry();
    invalidate();
  }

  /* -------------------------------------------------------------- camera -- */

  function fit() {
    state.target = state.centre.slice();
    const fov = 35 * Math.PI / 180;
    // The bounding sphere has to fit the narrower of the two fields of view, or
    // a tall thin window clips the model off at the sides.
    const aspect = canvas.clientWidth / Math.max(1, canvas.clientHeight);
    const horizontal = 2 * Math.atan(Math.tan(fov / 2) * aspect);
    state.distance = (state.radius / Math.sin(Math.min(fov, horizontal) / 2)) * 1.06;
    invalidate();
  }

  // Azimuth and elevation for each named view. The two tables differ because
  // "front" is -Y to a Z-up model and +Z to a Y-up one, and the orbit angles are
  // measured against whichever axis is up.
  const VIEWS = {
    z: {
      iso: [-0.90, 1.05], front: [-HALF_PI, HALF_PI], back: [HALF_PI, HALF_PI],
      right: [0, HALF_PI], left: [Math.PI, HALF_PI],
      top: [-HALF_PI, 0.001], bottom: [-HALF_PI, Math.PI - 0.001],
    },
    y: {
      iso: [0.70, 1.05], front: [0, HALF_PI], back: [Math.PI, HALF_PI],
      right: [HALF_PI, HALF_PI], left: [-HALF_PI, HALF_PI],
      top: [0, 0.001], bottom: [0, Math.PI - 0.001],
    },
  };

  function setView(name) {
    const v = VIEWS[state.zUp ? 'z' : 'y'][name];
    if (!v) return;
    state.theta = v[0];
    state.phi = v[1];
    invalidate();
  }

  function upVector() { return state.zUp ? [0, 0, 1] : [0, 1, 0]; }

  function cameraBasis() {
    const st = Math.sin(state.phi), ct = Math.cos(state.phi);
    const dir = state.zUp
      ? [st * Math.cos(state.theta), st * Math.sin(state.theta), ct]
      : [st * Math.sin(state.theta), ct, st * Math.cos(state.theta)];
    const eye = [
      state.target[0] + dir[0] * state.distance,
      state.target[1] + dir[1] * state.distance,
      state.target[2] + dir[2] * state.distance,
    ];
    return { eye, dir };
  }

  /* ---------------------------------------------------------- interaction -- */

  let drag = null;

  canvas.addEventListener('pointerdown', (e) => {
    canvas.setPointerCapture(e.pointerId);
    drag = { x: e.clientX, y: e.clientY, pan: e.button === 1 || e.button === 2 || e.shiftKey };
    e.preventDefault();
  });

  canvas.addEventListener('pointermove', (e) => {
    if (!drag) return;
    const dx = e.clientX - drag.x, dy = e.clientY - drag.y;
    drag.x = e.clientX; drag.y = e.clientY;

    if (drag.pan) {
      // Pan in the camera plane, scaled so a pixel moves the same amount of
      // model whatever the zoom.
      const { eye } = cameraBasis();
      const f = [state.target[0] - eye[0], state.target[1] - eye[1], state.target[2] - eye[2]];
      const n = Math.hypot(...f); f.forEach((_, i) => f[i] /= n);
      const up = upVector();
      let r = [f[1] * up[2] - f[2] * up[1], f[2] * up[0] - f[0] * up[2], f[0] * up[1] - f[1] * up[0]];
      const rn = Math.hypot(...r) || 1; r = r.map((v) => v / rn);
      const u = [r[1] * f[2] - r[2] * f[1], r[2] * f[0] - r[0] * f[2], r[0] * f[1] - r[1] * f[0]];

      const scale = 2 * state.distance * Math.tan(35 * Math.PI / 360) / canvas.clientHeight;
      for (let i = 0; i < 3; i++) state.target[i] += (-dx * r[i] + dy * u[i]) * scale;
    } else {
      state.theta -= dx * 0.008;
      state.phi = Math.min(Math.PI - 0.001, Math.max(0.001, state.phi - dy * 0.008));
    }
    invalidate();
  });

  const endDrag = () => { drag = null; };
  canvas.addEventListener('pointerup', endDrag);
  canvas.addEventListener('pointercancel', endDrag);
  canvas.addEventListener('contextmenu', (e) => e.preventDefault());

  canvas.addEventListener('wheel', (e) => {
    e.preventDefault();
    const k = Math.exp((e.deltaMode === 1 ? e.deltaY * 16 : e.deltaY) * 0.0012);
    state.distance = Math.min(state.radius * 200, Math.max(state.radius * 0.01, state.distance * k));
    invalidate();
  }, { passive: false });

  canvas.addEventListener('dblclick', fit);

  /* -------------------------------------------------------------- render -- */

  function resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const w = Math.max(1, Math.round(canvas.clientWidth * dpr));
    const h = Math.max(1, Math.round(canvas.clientHeight * dpr));
    if (canvas.width !== w || canvas.height !== h) {
      canvas.width = w;
      canvas.height = h;
      return true;
    }
    return false;
  }

  function clipPlane() {
    const n = [0, 0, 0];
    n[state.clipAxis] = state.clipFlip ? -1 : 1;
    const lo = state.min[state.clipAxis], hi = state.max[state.clipAxis];
    const pad = (hi - lo) * 0.001 + 1e-9;
    const at = lo - pad + (hi - lo + 2 * pad) * state.clipT;
    return [n[0], n[1], n[2], state.clipFlip ? -at : at];
  }

  function render() {
    resize();
    gl.viewport(0, 0, canvas.width, canvas.height);
    gl.disable(gl.DEPTH_TEST);
    gl.depthMask(false);

    gl.useProgram(bgProg);
    gl.uniform3f(uniform(gl, bgProg, 'uTop'), 0.098, 0.106, 0.129);
    gl.uniform3f(uniform(gl, bgProg, 'uBottom'), 0.043, 0.047, 0.059);
    gl.bindVertexArray(emptyVao);
    gl.drawArrays(gl.TRIANGLES, 0, 3);

    gl.enable(gl.DEPTH_TEST);
    gl.depthMask(true);
    gl.clear(gl.DEPTH_BUFFER_BIT);

    if (!state.vao) { drawGizmo(); return; }

    const aspect = canvas.width / Math.max(1, canvas.height);
    const near = Math.max(state.radius * 0.001, state.distance * 0.005);
    const far = state.distance + state.radius * 6;
    perspective(proj, 35 * Math.PI / 180, aspect, near, far);

    // The up axis lives in the view matrix. Nothing else needs to know about
    // it: the shader lights in eye space, and the clip plane is applied to the
    // untransformed position, so it stays in model units.
    const { eye } = cameraBasis();
    lookAt(view, eye, state.target, upVector());

    gl.useProgram(meshProg);
    gl.uniformMatrix4fv(uniform(gl, meshProg, 'uProj'), false, proj);
    gl.uniformMatrix4fv(uniform(gl, meshProg, 'uView'), false, view);
    gl.uniform3fv(uniform(gl, meshProg, 'uColor'), state.color);
    gl.uniform3fv(uniform(gl, meshProg, 'uBackColor'), state.backColor);
    gl.uniform1i(uniform(gl, meshProg, 'uFlat'), state.flat ? 1 : 0);
    gl.uniform1i(uniform(gl, meshProg, 'uClipOn'), state.clipOn ? 1 : 0);
    gl.uniform4fv(uniform(gl, meshProg, 'uClip'), clipPlane());
    gl.uniform1f(uniform(gl, meshProg, 'uOpacity'), 1);

    gl.bindVertexArray(state.vao);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, state.idxBuf);
    gl.drawElements(gl.TRIANGLES, state.triangles * 3, gl.UNSIGNED_INT, 0);

    if (state.wireframe && state.edgeBuf) {
      gl.useProgram(lineProg);
      gl.uniformMatrix4fv(uniform(gl, lineProg, 'uProj'), false, proj);
      gl.uniformMatrix4fv(uniform(gl, lineProg, 'uView'), false, view);
      gl.uniform4f(uniform(gl, lineProg, 'uColor'), 0.04, 0.05, 0.07, 1);
      gl.uniform1f(uniform(gl, lineProg, 'uDepthNudge'), 0.0008);
      gl.bindVertexArray(state.vao);
      gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, state.edgeBuf);
      gl.drawElements(gl.LINES, state.edgeCount, gl.UNSIGNED_INT, 0);
    }

    if (state.showBox && state.boxBuf) {
      gl.useProgram(lineProg);
      gl.uniformMatrix4fv(uniform(gl, lineProg, 'uProj'), false, proj);
      gl.uniformMatrix4fv(uniform(gl, lineProg, 'uView'), false, view);
      gl.uniform4f(uniform(gl, lineProg, 'uColor'), 0.35, 0.55, 0.75, 1);
      gl.uniform1f(uniform(gl, lineProg, 'uDepthNudge'), 0);
      const vao = gl.createVertexArray();
      gl.bindVertexArray(vao);
      gl.bindBuffer(gl.ARRAY_BUFFER, state.boxBuf);
      gl.enableVertexAttribArray(0);
      gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 0, 0);
      gl.drawArrays(gl.LINES, 0, 24);
      gl.bindVertexArray(null);
      gl.deleteVertexArray(vao);
    }

    gl.bindVertexArray(null);
    drawGizmo();
  }

  /* --------------------------------------------------------------- gizmo -- */

  const AXES = [
    { v: [1, 0, 0], label: 'X', color: '#e8615f' },
    { v: [0, 1, 0], label: 'Y', color: '#79c46b' },
    { v: [0, 0, 1], label: 'Z', color: '#5b9bf0' },
  ];

  function drawGizmo() {
    if (!gizmoSvg) return;
    const { eye } = cameraBasis();
    const f = [state.target[0] - eye[0], state.target[1] - eye[1], state.target[2] - eye[2]];
    const n = Math.hypot(...f) || 1;
    const fwd = f.map((v) => v / n);
    const up = upVector();
    let right = [fwd[1] * up[2] - fwd[2] * up[1], fwd[2] * up[0] - fwd[0] * up[2], fwd[0] * up[1] - fwd[1] * up[0]];
    const rn = Math.hypot(...right) || 1; right = right.map((v) => v / rn);
    const camUp = [right[1] * fwd[2] - right[2] * fwd[1], right[2] * fwd[0] - right[0] * fwd[2], right[0] * fwd[1] - right[1] * fwd[0]];

    const cx = 34, cy = 34, r = 22;
    const parts = AXES.map((a) => {
      const x = cx + (a.v[0] * right[0] + a.v[1] * right[1] + a.v[2] * right[2]) * r;
      const y = cy - (a.v[0] * camUp[0] + a.v[1] * camUp[1] + a.v[2] * camUp[2]) * r;
      const depth = a.v[0] * fwd[0] + a.v[1] * fwd[1] + a.v[2] * fwd[2];
      return { ...a, x, y, depth };
    }).sort((p, q) => q.depth - p.depth);

    gizmoSvg.innerHTML = parts.map((p) => {
      const dim = p.depth > 0 ? 0.45 : 1;
      return `<line x1="${cx}" y1="${cy}" x2="${p.x.toFixed(2)}" y2="${p.y.toFixed(2)}" `
           + `stroke="${p.color}" stroke-opacity="${dim}" stroke-width="1.5" stroke-linecap="round"/>`
           + `<circle cx="${p.x.toFixed(2)}" cy="${p.y.toFixed(2)}" r="5.5" fill="${p.color}" fill-opacity="${dim}"/>`
           + `<text x="${p.x.toFixed(2)}" y="${(p.y + 2.6).toFixed(2)}" text-anchor="middle" `
           + `font-size="7" font-family="ui-monospace, monospace" fill="#12141a" fill-opacity="${dim}">${p.label}</text>`;
    }).join('');
  }

  /* ------------------------------------------------------------- exports -- */

  const observer = new ResizeObserver(() => invalidate());
  observer.observe(canvas);

  function set(patch) {
    Object.assign(state, patch);
    if (patch.wireframe) ensureEdges();
    invalidate();
  }

  function screenshot(name) {
    render();
    canvas.toBlob((blob) => {
      if (!blob) return;
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = name || 'pltp.png';
      a.click();
      setTimeout(() => URL.revokeObjectURL(url), 5000);
    }, 'image/png');
  }

  invalidate();

  return {
    setMesh, clear, fit, setView, set, screenshot, invalidate,
    // Draws immediately instead of waiting for the next frame. The screenshot
    // path needs it, and so does anything driving the page without a compositor.
    redraw: render,
    get state() { return state; },
    get wireframeAvailable() { return state.triangles > 0 && state.triangles <= EDGE_LIMIT; },
  };
}

/* --------------------------------------------------------------- helpers -- */

function shader(gl, type, src) {
  const s = gl.createShader(type);
  gl.shaderSource(s, src);
  gl.compileShader(s);
  if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s) || 'shader');
  return s;
}

function program(gl, vs, fs) {
  const p = gl.createProgram();
  gl.attachShader(p, shader(gl, gl.VERTEX_SHADER, vs));
  gl.attachShader(p, shader(gl, gl.FRAGMENT_SHADER, fs));
  gl.linkProgram(p);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(p) || 'link');
  p._loc = new Map();
  return p;
}

// Uniform locations never change for the life of a program, so they are looked
// up once and kept on it.
function uniform(gl, p, name) {
  if (!p._loc.has(name)) p._loc.set(name, gl.getUniformLocation(p, name));
  return p._loc.get(name);
}

function buffer_(gl, target, data) {
  const b = gl.createBuffer();
  gl.bindBuffer(target, b);
  gl.bufferData(target, data, gl.STATIC_DRAW);
  return b;
}
