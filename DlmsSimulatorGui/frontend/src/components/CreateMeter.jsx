import { useEffect, useRef, useState } from 'react';
import { api } from '../api.js';

export default function CreateMeter({ templates, onCreate, onUploaded }) {
  const [name, setName] = useState('');
  const [port, setPort] = useState(4061);
  const [serial, setSerial] = useState(1);
  const [template, setTemplate] = useState('');
  const [useLogicalName, setUseLogicalName] = useState(true);
  const [iface, setIface] = useState('WRAPPER');
  const fileRef = useRef(null);

  useEffect(() => {
    if (!template && templates.length) setTemplate(templates[0]);
  }, [templates, template]);

  function submit(e) {
    e.preventDefault();
    onCreate({
      name,
      port: Number(port),
      serial: Number(serial),
      template,
      useLogicalName,
      interface: iface,
    });
  }

  async function upload(e) {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const r = await api.uploadTemplate(file);
      await onUploaded();
      setTemplate(r.name);
    } catch (err) {
      alert('Upload failed: ' + err.message);
    }
    if (fileRef.current) fileRef.current.value = '';
  }

  return (
    <form className="card create-form" onSubmit={submit}>
      <h3>New meter</h3>

      <label>Name</label>
      <input value={name} placeholder="e.g. Feeder 1" onChange={(e) => setName(e.target.value)} />

      <div className="row">
        <div>
          <label>TCP port</label>
          <input type="number" min="1" max="65535" value={port} onChange={(e) => setPort(e.target.value)} />
        </div>
        <div>
          <label>Serial no.</label>
          <input type="number" min="1" value={serial} onChange={(e) => setSerial(e.target.value)} />
        </div>
      </div>

      <label>Template</label>
      <div className="row template-row">
        <select value={template} onChange={(e) => setTemplate(e.target.value)}>
          {templates.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        <button type="button" className="ghost" onClick={() => fileRef.current?.click()} title="Upload template XML">
          ⬆
        </button>
        <input ref={fileRef} type="file" accept=".xml" hidden onChange={upload} />
      </div>

      <div className="row">
        <div>
          <label>Referencing</label>
          <select value={useLogicalName ? 'ln' : 'sn'} onChange={(e) => setUseLogicalName(e.target.value === 'ln')}>
            <option value="ln">Logical Name</option>
            <option value="sn">Short Name</option>
          </select>
        </div>
        <div>
          <label>Interface</label>
          <select value={iface} onChange={(e) => setIface(e.target.value)}>
            <option value="WRAPPER">WRAPPER (TCP)</option>
            <option value="HDLC">HDLC</option>
          </select>
        </div>
      </div>

      <button type="submit" className="primary" disabled={!template}>
        + Add meter
      </button>
    </form>
  );
}
