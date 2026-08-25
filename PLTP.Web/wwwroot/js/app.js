import { createViewer } from './viewer.js';

const $ = (id) => document.getElementById(id);

const ui = {
  samples: $('samples'), sampleNote: $('sample-note'),
  fileModel: $('file-model'), fileSen: $('file-sen'),
  dropModel: $('drop-model'), dropSen: $('drop-sen'),
  run: $('run'), cancel: $('cancel'),
  progress: $('progress'), stage: $('stage'), elapsed: $('elapsed'), fill: $('bar-fill'),
  log: $('log'), clearLog: $('clear-log'),
  empty: $('empty'), stats: $('stats'), downloads: $('downloads'),
  dlObj: $('dl-obj'), dlStl: $('dl-stl'),
  clipBar: $('clip-bar'), clipT: $('clip-t'),
};

let viewer;
try {
  viewer = createViewer($('canvas'), $('gizmo'));
} catch (e) {
  $('empty').innerHTML = `<h3>Cannot draw</h3><p>${e.message}</p>`;
}

// Reachable from the console: handy when something looks wrong in the viewer
// and the only way to ask is from the page itself.
window.pltp = { get viewer() { return viewer; } };

let source = 'sample';
let chosenSample = null;
let poll = null;
let currentJob = null;

/* ------------------------------------------------------------------ log -- */

function line(text, level = 'info') {
  const el = document.createElement('div');
  if (level !== 'info') el.className = level;
  el.textContent = text;
  const atBottom = ui.log.scrollHeight - ui.log.scrollTop - ui.log.clientHeight < 30;
  ui.log.appendChild(el);
  if (atBottom) ui.log.scrollTop = ui.log.scrollHeight;
}

ui.clearLog.addEventListener('click', () => { ui.log.textContent = ''; });

/* -------------------------------------------------------------- samples -- */

async function loadSamples() {
  let data;
  try {
    data = await (await fetch('api/samples')).json();
  } catch {
    ui.sampleNote.textContent = 'Could not reach the server.';
    return;
  }

  if (data.paperUrl) {
    $('cite-pdf').href = data.paperUrl;
    $('cite-pdf').classList.remove('hidden');
  }

  if (!data.samples || data.samples.length === 0) {
    ui.sampleNote.textContent = data.dataRoot
      ? 'The data folder was found but holds none of the known samples.'
      : 'No data/ folder next to the app. Use the other tab and upload your own files.';
    document.querySelector('.tab[data-source="upload"]').click();
    return;
  }

  ui.sampleNote.textContent = `From ${data.dataRoot}`;

  for (const s of data.samples) {
    const card = document.createElement('button');
    card.className = 'sample';
    card.type = 'button';
    card.innerHTML =
      `<span><b></b><small></small></span>` +
      `<span class="badge"></span>`;
    card.querySelector('b').textContent = s.name;
    card.querySelector('small').textContent = s.description;
    card.querySelector('.badge').textContent =
      `${s.elementType === 'hex' ? 'HEX' : 'TET'} ${compact(s.elements)}`;

    card.addEventListener('click', () => {
      document.querySelectorAll('.sample').forEach((c) => c.classList.remove('is-on'));
      card.classList.add('is-on');
      chosenSample = s.id;
      // The filter radius is a length, so it belongs to the mesh rather than to
      // the app - each sample brings its own.
      $('volumeFraction').value = s.volumeFraction;
      $('filterRadius').value = s.filterRadius;
      $('isovalue').value = s.isovalue;
      syncRanges();
      updateRun();
    });

    ui.samples.appendChild(card);
  }
}

const compact = (n) => n >= 1000 ? `${Math.round(n / 1000)}k` : `${n}`;

/* --------------------------------------------------------------- source -- */

document.querySelectorAll('.tab').forEach((tab) => {
  tab.addEventListener('click', () => {
    document.querySelectorAll('.tab').forEach((t) => t.classList.remove('is-on'));
    tab.classList.add('is-on');
    source = tab.dataset.source;
    $('source-sample').classList.toggle('hidden', source !== 'sample');
    $('source-upload').classList.toggle('hidden', source !== 'upload');
    updateRun();
  });
});

function wireDrop(drop, input) {
  const sub = drop.querySelector('.drop-sub');
  const show = () => {
    const f = input.files && input.files[0];
    drop.classList.toggle('has-file', !!f);
    sub.textContent = f ? `${f.name}  ·  ${bytes(f.size)}` : sub.dataset.empty;
    updateRun();
  };
  input.addEventListener('change', show);

  ['dragenter', 'dragover'].forEach((ev) =>
    drop.addEventListener(ev, (e) => { e.preventDefault(); drop.classList.add('over'); }));
  ['dragleave', 'drop'].forEach((ev) =>
    drop.addEventListener(ev, (e) => { e.preventDefault(); drop.classList.remove('over'); }));

  drop.addEventListener('drop', (e) => {
    if (!e.dataTransfer.files.length) return;
    input.files = e.dataTransfer.files;
    show();
  });
}

wireDrop(ui.dropModel, ui.fileModel);
wireDrop(ui.dropSen, ui.fileSen);

const bytes = (n) =>
  n < 1024 ? `${n} B`
  : n < 1048576 ? `${(n / 1024).toFixed(0)} kB`
  : `${(n / 1048576).toFixed(1)} MB`;

/* ----------------------------------------------------------- parameters -- */

function pair(numberId) {
  const num = $(numberId);
  const range = $(`${numberId}-range`);
  if (!range) return;
  range.addEventListener('input', () => { num.value = range.value; });
  num.addEventListener('input', () => { range.value = num.value; });
}
pair('volumeFraction');
pair('isovalue');

function syncRanges() {
  for (const id of ['volumeFraction', 'isovalue']) {
    const r = $(`${id}-range`);
    if (r) r.value = $(id).value;
  }
}

$('keepVolume').addEventListener('change', volumeMode);
function volumeMode() {
  const on = $('keepVolume').checked;
  $('field-vf').classList.toggle('hidden', !on);
  $('field-iso').classList.toggle('hidden', on);
  $('tolerance').closest('.grid-2').classList.toggle('hidden', !on);
}
volumeMode();

function updateRun() {
  const ready = source === 'sample'
    ? !!chosenSample
    : !!(ui.fileModel.files[0] && ui.fileSen.files[0]);
  ui.run.disabled = !ready || currentJob !== null;
}

/* ------------------------------------------------------------------ run -- */

ui.run.addEventListener('click', async () => {
  const form = new FormData();

  if (source === 'sample') form.append('sample', chosenSample);
  else {
    form.append('model', ui.fileModel.files[0]);
    form.append('sensitivity', ui.fileSen.files[0]);
  }

  for (const id of ['volumeFraction', 'isovalue', 'filterRadius', 'tolerance',
                    'maximumIteration', 'weldTolerance', 'elementType', 'format',
                    'sensitivityKind'])
    form.append(id, $(id).value);

  for (const id of ['interpolation', 'keepVolume', 'normalize', 'keepLargestComponent'])
    form.append(id, $(id).checked ? 'true' : 'false');

  ui.log.textContent = '';
  ui.stats.classList.add('hidden');
  ui.downloads.classList.add('hidden');
  ui.progress.classList.remove('hidden', 'is-error', 'is-done');
  ui.fill.style.width = '0%';
  ui.stage.textContent = 'submitting';
  ui.run.disabled = true;
  ui.cancel.classList.remove('hidden');

  let res;
  try {
    res = await (await fetch('api/jobs', { method: 'POST', body: form })).json();
  } catch (e) {
    return fail(`Could not reach the server: ${e.message}`);
  }
  if (res.error) return fail(res.error);

  currentJob = res.id;
  watch(res.id, 0);
});

ui.cancel.addEventListener('click', async () => {
  if (!currentJob) return;
  ui.cancel.disabled = true;
  try { await fetch(`api/jobs/${currentJob}/cancel`, { method: 'POST' }); } catch { /* it will end anyway */ }
});

function fail(message) {
  line(message, 'error');
  ui.progress.classList.add('is-error');
  ui.stage.textContent = 'failed';
  ui.fill.style.width = '100%';
  finish();
}

function finish() {
  currentJob = null;
  clearTimeout(poll);
  poll = null;
  ui.cancel.classList.add('hidden');
  ui.cancel.disabled = false;
  updateRun();
}

async function watch(id, since) {
  let s;
  try {
    s = await (await fetch(`api/jobs/${id}?since=${since}`)).json();
  } catch (e) {
    return fail(`Lost contact with the job: ${e.message}`);
  }
  if (s.error && !s.state) return fail(s.error);

  for (const entry of s.log || [])
    line(`${entry.t.toFixed(1).padStart(5)}s  ${entry.text}`, entry.level);

  ui.stage.textContent = s.stage;
  ui.elapsed.textContent = `${(s.elapsedMs / 1000).toFixed(1)}s`;
  ui.fill.style.width = `${Math.round(s.progress * 100)}%`;

  if (s.state === 'completed') {
    ui.progress.classList.add('is-done');
    await show(id, s.result);
    finish();
    return;
  }
  if (s.state === 'failed') {
    ui.progress.classList.add('is-error');
    finish();
    return;
  }
  if (s.state === 'cancelled') {
    ui.stage.textContent = 'cancelled';
    finish();
    return;
  }

  poll = setTimeout(() => watch(id, s.logCount), 350);
}

/* -------------------------------------------------------------- results -- */

async function show(id, result) {
  if (!viewer) return;

  if (!result || result.triangles === 0) {
    viewer.clear();
    ui.empty.classList.remove('hidden');
    return;
  }

  const buffer = await (await fetch(`api/jobs/${id}/mesh`)).arrayBuffer();
  viewer.setMesh(buffer);
  ui.empty.classList.add('hidden');

  $('t-wire').disabled = !viewer.wireframeAvailable;

  const size = result.max.map((v, i) => v - result.min[i]);
  const rows = [
    ['vertices', result.vertices.toLocaleString()],
    ['faces', result.faces.toLocaleString()],
    ['triangles', result.triangles.toLocaleString()],
    ['isovalue', result.isovalue.toPrecision(5)],
    ['volume', result.volume.toPrecision(6)],
    ['of the mesh', `${(result.volumeFraction * 100).toFixed(2)}%`],
    ['bisection', result.iterations ? `${result.iterations} trials` : 'off'],
    ['input', `${result.elementCount.toLocaleString()} ${result.elementType}`],
    ['field', result.sensitivityKind],
    ['size', size.map((v) => v.toPrecision(4)).join(' × ')],
    ['took', `${(result.elapsedMs / 1000).toFixed(2)} s`],
  ];

  ui.stats.replaceChildren(...rows.flatMap(([k, v]) => {
    const key = document.createElement('span');
    key.className = 'k';
    key.textContent = k;
    const val = document.createElement('span');
    val.className = 'v';
    val.textContent = v;
    return [key, val];
  }));

  if (result.droppedFaces > 0) {
    const note = document.createElement('span');
    note.className = 'note';
    note.textContent = `${result.droppedFaces.toLocaleString()} faces dropped as detached pieces.`;
    ui.stats.appendChild(note);
  }

  ui.stats.classList.remove('hidden');
  ui.dlObj.href = `api/jobs/${id}/download/obj`;
  ui.dlStl.href = `api/jobs/${id}/download/stl`;
  ui.downloads.classList.remove('hidden');

  // The section slider spans the model, so it has to start at the far end -
  // otherwise turning sectioning on hides everything.
  ui.clipT.value = 1;
  viewer.set({ clipT: 1 });
}

/* -------------------------------------------------------------- toolbar -- */

document.querySelectorAll('[data-view]').forEach((b) =>
  b.addEventListener('click', () => viewer && viewer.setView(b.dataset.view)));

$('t-fit').addEventListener('click', () => viewer && viewer.fit());

$('t-up').addEventListener('click', (e) => {
  if (!viewer) return;
  const zUp = !viewer.state.zUp;
  viewer.set({ zUp });
  // The orbit angles are measured against the up axis, so carrying them over
  // would land the camera somewhere arbitrary. Go back to the isometric.
  viewer.setView('iso');
  e.currentTarget.textContent = zUp ? 'Z up' : 'Y up';
});

$('t-shading').addEventListener('click', (e) => {
  if (!viewer) return;
  const flat = !viewer.state.flat;
  viewer.set({ flat });
  e.currentTarget.textContent = flat ? 'Flat' : 'Smooth';
});

toggle('t-wire', (on) => viewer.set({ wireframe: on }));
toggle('t-box', (on) => viewer.set({ showBox: on }));

toggle('t-clip', (on) => {
  viewer.set({ clipOn: on });
  ui.clipBar.classList.toggle('hidden', !on);
});

function toggle(id, apply) {
  const b = $(id);
  b.addEventListener('click', () => {
    if (!viewer) return;
    const on = !b.classList.contains('is-on');
    b.classList.toggle('is-on', on);
    apply(on);
  });
}

document.querySelectorAll('[data-axis]').forEach((b) =>
  b.addEventListener('click', () => {
    document.querySelectorAll('[data-axis]').forEach((o) => o.classList.remove('is-on'));
    b.classList.add('is-on');
    viewer && viewer.set({ clipAxis: +b.dataset.axis });
  }));

ui.clipT.addEventListener('input', () => viewer && viewer.set({ clipT: +ui.clipT.value }));

$('clip-flip').addEventListener('click', (e) => {
  if (!viewer) return;
  const flip = !viewer.state.clipFlip;
  viewer.set({ clipFlip: flip });
  e.currentTarget.classList.toggle('is-on', flip);
});

$('t-shot').addEventListener('click', () => viewer && viewer.screenshot('pltp.png'));

/* ----------------------------------------------------------------- cite -- */

const BIBTEX = `@article{li2022smoothing,
  title   = {Smoothing topology optimization results using pre-built lookup tables},
  author  = {Li, Zhi and Lee, Ting-Uei and Yao, Yuan and Xie, Yi Min},
  journal = {Advances in Engineering Software},
  volume  = {173},
  pages   = {103204},
  year    = {2022},
  issn    = {0965-9978},
  doi     = {10.1016/j.advengsoft.2022.103204}
}`;

$('cite-bib').addEventListener('click', (e) => {
  const button = e.currentTarget;
  const copied = copyText(BIBTEX);

  // Never awaited: navigator.clipboard needs a focused document and, in a
  // background window, can simply never settle - awaiting it left the button
  // dead with nothing copied and no error to show for it.
  button.textContent = copied ? 'Copied' : 'Select it below';
  button.classList.toggle('copied', copied);
  if (!copied) $('cite-bib-text').open = true;

  setTimeout(() => {
    button.textContent = 'Copy BibTeX';
    button.classList.remove('copied');
  }, 1800);
});

/// Synchronous, so the button can say truthfully whether it worked. execCommand
/// is deprecated but it is the only path that reports success in the same tick;
/// the async API is kicked off behind it for browsers that have dropped it.
function copyText(text) {
  let ok = false;
  const box = document.createElement('textarea');
  box.value = text;
  box.style.cssText = 'position:fixed;top:0;left:0;opacity:0';
  document.body.appendChild(box);
  box.select();
  try { ok = document.execCommand('copy'); } catch { ok = false; }
  box.remove();

  if (!ok && navigator.clipboard?.writeText) {
    navigator.clipboard.writeText(text).then(
      () => { /* landed late, the fallback below is harmless */ },
      () => { /* no clipboard here; the text is on screen instead */ });
  }
  return ok;
}

/* ----------------------------------------------------------------- boot -- */

$('t-wire').disabled = true;
loadSamples().then(updateRun);
updateRun();
