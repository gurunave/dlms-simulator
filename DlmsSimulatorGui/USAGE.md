# DLMS Simulator — User Guide

How to use the DLMS Simulator GUI to create virtual DLMS/COSEM meters, edit
their values, and connect real DLMS clients to them. No physical meter required.

For architecture and developer setup, see [README.md](README.md).

---

## 1. Start the app

You need the meter engine (backend) running. From `DlmsSimulatorGui/backend`:

```bash
dotnet run --urls http://localhost:5100
```

Then open **http://localhost:5100** in your browser.

> First launch seeds a set of ready-made meter templates automatically, so you
> can create a meter straight away.

---

## 2. The screen at a glance

The window has three panes:

```
┌───────────────┬────────────────────────────┬──────────────────┐
│  LEFT         │  CENTER                    │  RIGHT           │
│  New meter    │  COSEM objects of the      │  Live activity   │
│  form +       │  selected meter            │  (client reads,  │
│  Meter list   │  (view / edit values)      │   connections)   │
└───────────────┴────────────────────────────┴──────────────────┘
```

The badge at the top-right shows the real-time link status
(**connected** = live updates are flowing).

---

## 3. Create a meter

In the **New meter** panel (left):

| Field | What it means |
|-------|---------------|
| **Name** | A label for you (e.g. "Feeder 1"). |
| **TCP port** | The port the meter listens on. Each meter needs a unique port. `4061` is the DLMS default. |
| **Serial no.** | The meter serial number. It is baked into the logical device name and serial-number objects. |
| **Template** | Which COSEM object set to load (see [Templates](#7-templates)). |
| **Referencing** | **Logical Name** (modern, LN) or **Short Name** (SN). Most meters use Logical Name. |
| **Interface** | **WRAPPER (TCP)** for TCP/IP, or **HDLC** for serial-style framing over TCP. |

Click **+ Add meter**. The meter appears in the list below in the **Stopped**
state and is selected so you can see its objects.

---

## 4. Start / stop / delete

Each meter row has controls (hover to reveal):

- **▶ Start** — opens the TCP port and begins serving DLMS. The status dot turns
  **green** and the meter starts accepting client connections.
- **■ Stop** — closes the port and disconnects clients. Dot turns grey.
- **✕ Delete** — removes the meter entirely.

A running meter shows a live **client count** in its row when clients are connected.

---

## 5. View and edit COSEM objects

Select a meter to load its objects in the center pane. Each row is one
attribute of one COSEM object:

- **Type** — object type (Data, Register, Clock, AssociationLogicalName, …).
- **OBIS (Logical Name)** — the object's OBIS code, e.g. `0.0.1.0.0.255`.
- **Attr** — the attribute number (attribute 1 is always the Logical Name).
- **Value** — the current value.

**To edit a value** (meter must be **Running**):

1. Click the **✎** icon on the attribute row.
2. Type the new value and press **Enter** (or click **✔**). **Esc** / **✕** cancels.
3. The change takes effect immediately for any client that reads it, and is
   saved back to the template file.

Notes:
- Attribute 1 (Logical Name) is read-only.
- When a meter is **Stopped**, values are shown from the template file and
  editing is disabled — start the meter to edit.
- Enter values in the attribute's natural form: numbers as numbers, text as
  text, byte strings as hex (e.g. `454C2D53494D`).
- Use the **filter box** to find an object by OBIS code or type, and **⟳** to
  refresh values.

---

## 6. Watch live activity

The right pane streams events in real time as clients talk to your meters:

| Icon | Event |
|------|-------|
| 🔗 | A client connected (shows its address). |
| 📤 | A client **read** an attribute (shows `OBIS:attr`). |
| ✏️ | A client **wrote** an attribute. |
| ⛔ | A client disconnected. |

This is the fastest way to confirm a head-end system or test tool is actually
reaching the meter and which objects it is polling. Use **Clear** to reset the list.

---

## 7. Templates

A **template** is an XML file describing a meter's full COSEM object model
(which objects exist and their default values). The simulator ships with many:

- `crystal.xml` — a small, simple meter. Good for first tests.
- `LN-v2-*` / `LN-v3-*` — Logical-Name meters with various security/authentication
  setups (None, Low, High, MD5, SHA-1, SHA-256, GMAC…).
- `GMac…` — meters using GMAC authenticated encryption.

**Upload your own:** click the **⬆** button next to the Template dropdown and
pick an `.xml` file. Template XML is the same format the official Gurux simulator
produces, so you can capture a real meter's model with the Gurux tools and drop
it in here.

---

## 8. Connect a DLMS client

Point any DLMS/COSEM client (a head-end system, Gurux tools, or the bundled
probe) at a **running** meter.

**Connection settings** (match how you created the meter):

| Setting | Value |
|---------|-------|
| Host / IP | The machine running the simulator (e.g. `127.0.0.1`) |
| Port | The meter's TCP port (e.g. `4061`) |
| Interface | WRAPPER (TCP) or HDLC — same as the meter |
| Referencing | Logical Name or Short Name — same as the meter |
| Client address | `16` (public client) for the sample templates |
| Server address | `1` |
| Authentication | Per the template (the `crystal.xml` sample uses **None**) |

**Quick self-test** with the bundled probe — with a meter running on port 4061:

```bash
cd tools/DlmsProbe
dotnet run 127.0.0.1 4061
```

It associates and reads the logical device name, serial number, and clock. You
should see those reads pop up in the **Live activity** pane as they happen.

---

## 9. Typical workflows

**Test a head-end/AMR system against many meters**
1. Create several meters, each on its own port (4061, 4062, 4063, …), sharing a
   template or using different ones.
2. Start them all.
3. Point your head-end at the ports and watch the activity pane to confirm polling.

**Reproduce a specific meter reading**
1. Start a meter, select it.
2. Edit the relevant register/attribute to the value you want to reproduce.
3. Have the client read it back — the edited value is served.

**Simulate different security setups**
- Pick the matching `LN-v2-*` / `LN-v3-*` template (Low/High/MD5/SHA/GMAC) and
  configure your client with the same authentication.

---

## 10. Troubleshooting

| Symptom | Likely cause / fix |
|---------|--------------------|
| "Port N is already assigned" | Another meter (or program) uses that port. Pick a different port. |
| Meter shows **Error** with a red dot | Start failed — usually the port is in use or the template is invalid. Hover the row for the message; try another port/template. |
| Client can't connect | Meter isn't **Running**; or client's port / interface / referencing don't match the meter. |
| Client connects but reads fail | Authentication or client/server address mismatch. The sample `crystal.xml` uses client `16`, server `1`, auth **None**. |
| Activity pane says "connecting" / disconnected | The backend isn't running or was restarted — reload the page after it's up. |
| Can't edit a value | The meter is Stopped (start it), or you're on attribute 1 (read-only). |

---

## 11. Stopping

Stop the backend process (Ctrl+C in its terminal) — this stops every simulated
meter and frees all their ports at once.
