// Thin wrapper around the backend REST API.
async function req(method, url, body) {
  const opts = { method, headers: {} };
  if (body !== undefined) {
    opts.headers['Content-Type'] = 'application/json';
    opts.body = JSON.stringify(body);
  }
  const res = await fetch(url, opts);
  if (!res.ok) {
    let msg = res.statusText;
    try {
      const j = await res.json();
      msg = j.error || msg;
    } catch {
      /* ignore */
    }
    throw new Error(msg);
  }
  if (res.status === 204) return null;
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

export const api = {
  templates: () => req('GET', '/api/templates'),
  uploadTemplate: async (file) => {
    const fd = new FormData();
    fd.append('file', file);
    const res = await fetch('/api/templates', { method: 'POST', body: fd });
    if (!res.ok) throw new Error((await res.text()) || 'Upload failed');
    return res.json();
  },
  meters: () => req('GET', '/api/meters'),
  createMeter: (m) => req('POST', '/api/meters', m),
  startMeter: (id) => req('POST', `/api/meters/${id}/start`),
  stopMeter: (id) => req('POST', `/api/meters/${id}/stop`),
  deleteMeter: (id) => req('DELETE', `/api/meters/${id}`),
  objects: (id) => req('GET', `/api/meters/${id}/objects`),
  setAttribute: (id, ln, index, value) =>
    req('PUT', `/api/meters/${id}/objects/${ln}`, { index, value }),
};
