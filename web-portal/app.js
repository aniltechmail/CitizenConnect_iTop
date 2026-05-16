const config = window.CITIZEN_CONNECT_CONFIG || {};
const state = {
  token: localStorage.getItem("cc_token"),
  user: JSON.parse(localStorage.getItem("cc_user") || "null"),
  apiBaseUrl: (config.apiBaseUrl || "").replace(/\/$/, "")
};

const statusValues = ["Submitted", "Assigned", "InProgress", "Resolved", "Closed", "Rejected", "Cancelled"];
const senderValues = ["Citizen", "Agent", "System"];
const roleValues = ["Admin", "Assigner", "FieldAgent", "Supervisor", "DepartmentHead", "TopManagement"];

function key(name) {
  return name.charAt(0).toLowerCase() + name.slice(1);
}

function normalize(value) {
  if (Array.isArray(value)) return value.map(normalize);
  if (!value || typeof value !== "object") return value;
  const result = {};
  for (const [rawKey, rawValue] of Object.entries(value)) {
    const camel = rawKey.includes("_")
      ? rawKey.replace(/_([a-z])/g, (_, c) => c.toUpperCase())
      : key(rawKey);
    result[camel] = normalize(rawValue);
  }
  return result;
}

function denormalize(value) {
  if (Array.isArray(value)) return value.map(denormalize);
  if (!value || typeof value !== "object") return value;
  const result = {};
  for (const [rawKey, rawValue] of Object.entries(value)) {
    result[rawKey] = denormalize(rawValue);
    const snake = rawKey.replace(/[A-Z]/g, c => `_${c.toLowerCase()}`);
    if (snake !== rawKey) result[snake] = denormalize(rawValue);
  }
  return result;
}

async function api(path, options = {}) {
  const headers = options.body instanceof FormData ? {} : { "Content-Type": "application/json" };
  if (state.token) headers.Authorization = `Bearer ${state.token}`;
  const response = await fetch(`${state.apiBaseUrl}${path}`, {
    ...options,
    headers: { ...headers, ...(options.headers || {}) },
    body: options.body instanceof FormData ? options.body : options.body ? JSON.stringify(denormalize(options.body)) : undefined
  });
  if (!response.ok) {
    const text = await response.text();
    let message = text;
    try {
      const parsed = JSON.parse(text);
      message = parsed.message || parsed.detail || text;
    } catch {}
    throw new Error(message || response.statusText || `HTTP ${response.status}`);
  }
  if (response.status === 204) return null;
  const contentType = response.headers.get("content-type") || "";
  if (contentType.includes("text/csv")) return response.text();
  return normalize(await response.json());
}

function setSession(auth) {
  state.token = auth.token;
  state.user = auth.user;
  localStorage.setItem("cc_token", state.token);
  localStorage.setItem("cc_user", JSON.stringify(state.user));
}

function logout() {
  localStorage.removeItem("cc_token");
  localStorage.removeItem("cc_user");
  state.token = null;
  state.user = null;
  navigate("/login");
}

function navigate(path) {
  history.pushState({}, "", path);
  render();
}

function setTitle(title, subtitle = "") {
  document.getElementById("pageTitle").textContent = title;
  document.getElementById("pageSubtitle").textContent = subtitle;
}

function alert(message, isError = false) {
  document.getElementById("alertHost").innerHTML = `<div class="alert ${isError ? "error" : ""}">${escapeHtml(message)}</div>`;
  setTimeout(() => (document.getElementById("alertHost").innerHTML = ""), 5000);
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, char => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
  }[char]));
}

function formData(form) {
  const result = Object.fromEntries(new FormData(form).entries());
  for (const [name, value] of Object.entries(result)) {
    const numeric = name.endsWith("Id") || name.endsWith("Hours") || ["status", "role", "rating", "priority"].includes(name);
    if (value === "") result[name] = null;
    else if (numeric && !Number.isNaN(Number(value))) result[name] = Number(value);
  }
  return result;
}

function options(items, valueKey = "id", labelKey = "name", selected = "") {
  return `<option value="">Select</option>${(items || []).map(item =>
    `<option value="${escapeHtml(item[valueKey])}" ${String(item[valueKey]) === String(selected) ? "selected" : ""}>${escapeHtml(item[labelKey])}</option>`
  ).join("")}`;
}

function statusBadge(status) {
  const bad = ["Rejected", "Cancelled"].includes(status);
  const warn = ["Submitted", "Assigned"].includes(status);
  return `<span class="pill ${bad ? "bad" : warn ? "warn" : ""}">${escapeHtml(status)}</span>`;
}

function layoutNav() {
  const user = state.user;
  const isInternal = user?.userType === "internal";
  const links = user
    ? isInternal
      ? [["/dashboard", "Dashboard"], ["/complaints", "Complaints"], ["/reports", "Reports"], ["/admin", "Admin"]]
      : [["/complaints/new", "New Complaint"], ["/complaints", "My Complaints"]]
    : [["/login", "Login"], ["/register", "Register"]];
  const path = location.pathname;
  document.getElementById("nav").innerHTML = links.map(([href, label]) =>
    `<a href="${href}" data-link class="${path === href ? "active" : ""}"><span>${label}</span></a>`
  ).join("");
  document.getElementById("sessionBox").innerHTML = user
    ? `<span class="pill">${escapeHtml(user.fullName || user.email || user.phone)}</span><button class="button secondary" id="logoutButton">Logout</button>`
    : `<a class="button secondary" href="/login" data-link>Login</a>`;
  document.getElementById("logoutButton")?.addEventListener("click", logout);
}

async function registerPage() {
  setTitle("Register", "Create a citizen account");
  const districts = await api("/api/location/districts");
  app.innerHTML = `<div class="panel"><form id="registerForm" class="form-grid">
    <label>Full name<input name="fullName" required></label>
    <label>Phone<input name="phone" required></label>
    <label>Email<input type="email" name="email"></label>
    <label>Password<input type="password" name="password" required minlength="6"></label>
    <label>District<select id="district">${options(districts)}</select></label>
    <label>Constituency<select id="constituency"></select></label>
    <label>Area<select id="area"></select></label>
    <label>Block<select name="blockId" id="block" required></select></label>
    <div class="wide row"><button class="button">Create account</button><a href="/login" data-link class="button secondary">Already registered</a></div>
  </form></div>`;
  bindLocationCascades();
  registerForm.onsubmit = async event => {
    event.preventDefault();
    try {
      const auth = await api("/api/auth/citizen/register", { method: "POST", body: formData(registerForm) });
      setSession(auth);
      navigate("/complaints/new");
    } catch (err) { alert(err.message, true); }
  };
}

async function loginPage() {
  setTitle("Login", "Citizen and internal user sign in");
  app.innerHTML = `<div class="grid two">
    <div class="panel"><h2>Citizen login</h2><form id="citizenLogin" class="grid">
      <label>Phone<input name="identifier" required></label>
      <label>Password<input type="password" name="password" required></label>
      <button class="button">Login as citizen</button>
    </form></div>
    <div class="panel"><h2>Internal login</h2><form id="internalLogin" class="grid">
      <label>Email<input name="identifier" type="email" required></label>
      <label>Password<input type="password" name="password" required></label>
      <button class="button">Login as internal user</button>
    </form></div>
  </div>`;
  for (const [form, path] of [[citizenLogin, "/api/auth/citizen/login"], [internalLogin, "/api/auth/internal/login"]]) {
    form.onsubmit = async event => {
      event.preventDefault();
      try {
        const auth = await api(path, { method: "POST", body: formData(form) });
        setSession(auth);
        navigate(auth.user.userType === "internal" ? "/dashboard" : "/complaints");
      } catch (err) { alert(err.message, true); }
    };
  }
}

async function bindLocationCascades(prefix = "") {
  const district = document.getElementById(`${prefix}district`);
  const constituency = document.getElementById(`${prefix}constituency`);
  const area = document.getElementById(`${prefix}area`);
  const block = document.getElementById(`${prefix}block`);
  if (!district || !constituency || !area || !block) return;
  district.onchange = async () => {
    constituency.innerHTML = options(district.value ? await api(`/api/location/districts/${district.value}/constituencies`) : []);
    area.innerHTML = block.innerHTML = "";
  };
  constituency.onchange = async () => {
    area.innerHTML = options(constituency.value ? await api(`/api/location/constituencies/${constituency.value}/areas`) : []);
    block.innerHTML = "";
  };
  area.onchange = async () => {
    block.innerHTML = options(area.value ? await api(`/api/location/areas/${area.value}/blocks`) : []);
  };
}

async function newComplaintPage() {
  requireUser("citizen");
  setTitle("New Complaint", "Submit a grievance with category and location");
  const [districts, categories] = await Promise.all([api("/api/location/districts"), api("/api/master/categories")]);
  app.innerHTML = `<div class="panel"><form id="complaintForm" class="form-grid">
    <label class="wide">Title<input name="title" required></label>
    <label class="wide">Description<textarea name="description" required></textarea></label>
    <label>Category<select name="categoryId" required>${options(categories)}</select></label>
    <label>Priority<select name="priority"><option value="1">Low</option><option value="2">Normal</option><option value="3">High</option><option value="4">Urgent</option></select></label>
    <label>District<select id="district">${options(districts)}</select></label>
    <label>Constituency<select id="constituency"></select></label>
    <label>Area<select id="area"></select></label>
    <label>Block<select name="blockId" id="block" required></select></label>
    <div class="wide"><button class="button">Submit complaint</button></div>
  </form></div>`;
  bindLocationCascades();
  complaintForm.onsubmit = async event => {
    event.preventDefault();
    try {
      const complaint = await api("/api/complaint", { method: "POST", body: formData(complaintForm) });
      navigate(`/complaints/${complaint.id}`);
    } catch (err) { alert(err.message, true); }
  };
}

async function complaintsPage() {
  requireUser();
  const internal = state.user.userType === "internal";
  setTitle(internal ? "Complaints" : "My Complaints", internal ? "Filter and work all open grievances" : "Track your submitted grievances");
  const [departments, blocks] = await Promise.all([api("/api/master/departments"), api("/api/master/blocks")]);
  app.innerHTML = `${internal ? `<div class="panel flat"><form id="filterForm" class="row">
    <select name="status">${options(statusValues.map((name, id) => ({ id, name })))}</select>
    <select name="departmentId">${options(departments)}</select>
    <select name="blockId">${options(blocks)}</select>
    <button class="button">Apply</button>
  </form></div>` : ""}
  <div id="complaintList"></div>`;
  const filterForm = document.getElementById("filterForm");
  async function load() {
    const query = internal ? new URLSearchParams(Object.entries(formData(filterForm)).filter(([, v]) => v)).toString() : "";
    const data = await api(internal ? `/api/complaint${query ? `?${query}` : ""}` : "/api/complaint/my");
    const items = Array.isArray(data) ? data : data.items || [];
    complaintList.innerHTML = table(["Ref", "Title", "Status", "Category", "Department", "Created", ""], items.map(c => [
      c.refNumber, c.title, statusBadge(c.status), c.categoryName || c.category, c.assignedDepartmentName || c.department || "", formatDate(c.createdAt),
      `<a class="button secondary" href="/complaints/${c.id}" data-link>Open</a>`
    ]));
  }
  filterForm?.addEventListener("submit", event => { event.preventDefault(); load(); });
  await load();
}

async function complaintDetailPage(id) {
  requireUser();
  const [complaint, messages, departments, agents, escalations] = await Promise.all([
    api(`/api/complaint/${id}`),
    api(`/api/complaint/${id}/messages`).catch(() => []),
    api("/api/master/departments"),
    api("/api/master/agents").catch(() => []),
    api(`/api/complaint/${id}/escalations`).catch(() => [])
  ]);
  setTitle(complaint.refNumber || "Complaint", complaint.title);
  const internal = state.user.userType === "internal";
  app.innerHTML = `<div class="grid two">
    <div class="panel"><h2>${escapeHtml(complaint.title)}</h2>
      <p>${escapeHtml(complaint.description)}</p>
      <div class="row">${statusBadge(complaint.status)}<span class="pill">Priority ${escapeHtml(complaint.priority)}</span><span class="pill">${escapeHtml(complaint.categoryName)}</span></div>
      <p class="muted">Citizen: ${escapeHtml(complaint.citizenName)} ${escapeHtml(complaint.citizenPhone || "")}</p>
      <p class="muted">Block: ${escapeHtml(complaint.blockName)} | Department: ${escapeHtml(complaint.assignedDepartmentName || "Not assigned")}</p>
      <form id="mediaForm" class="row"><input type="file" name="file" required><button class="button secondary">Upload media</button></form>
      <div class="row">${(complaint.media || []).map(m => `<a class="pill" target="_blank" href="${escapeHtml(m.filePath || m.storagePath || "#")}">${escapeHtml(m.fileName || "Media")}</a>`).join("")}</div>
    </div>
    <div class="panel"><h2>Actions</h2>${internal ? internalActions(departments, agents, complaint) : `<a class="button" href="/complaints/${id}/feedback" data-link>Submit feedback</a>`}</div>
  </div>
  <div class="grid two" style="margin-top:16px">
    <div class="panel"><h2>Conversation</h2><div class="thread">${messages.length ? messages.map(messageCard).join("") : `<div class="empty">No messages yet.</div>`}</div>
      <form id="messageForm" class="grid" style="margin-top:12px"><textarea name="message" required placeholder="Write a message"></textarea><button class="button">Send message</button></form>
    </div>
    <div class="panel"><h2>Escalations</h2>${escalations.length ? table(["Level", "Reason", "Triggered"], escalations.map(e => [e.level, e.reason, formatDate(e.triggeredAt)])) : `<div class="empty">No escalations recorded.</div>`}</div>
  </div>`;
  mediaForm.onsubmit = async event => {
    event.preventDefault();
    try {
      await api(`/api/complaint/${id}/media`, { method: "POST", body: new FormData(mediaForm) });
      alert("Media uploaded.");
      complaintDetailPage(id);
    } catch (err) { alert(err.message, true); }
  };
  messageForm.onsubmit = async event => {
    event.preventDefault();
    try {
      await api(`/api/complaint/${id}/messages`, { method: "POST", body: formData(messageForm) });
      complaintDetailPage(id);
    } catch (err) { alert(err.message, true); }
  };
  bindInternalActionForms(id);
}

function internalActions(departments, agents, complaint) {
  const canAssign = ["Admin", "Assigner"].includes(state.user?.role);
  return `<div class="grid">
    ${canAssign ? `<form id="assignDeptForm" class="row"><select name="departmentId" required>${options(departments, "id", "name", complaint.assignedDepartmentId)}</select><button class="button">Assign department</button></form>
    <form id="assignAgentForm" class="row"><select name="agentId" required>${options(agents, "id", "fullName", complaint.assignedAgentId)}</select><button class="button">Assign agent</button></form>` : `<div class="empty">Assignment is available to Admin and Assigner roles.</div>`}
    <form id="statusForm" class="row"><select name="status">${options(statusValues.map((name, id) => ({ id, name })))}</select><button class="button">Update status</button></form>
  </div>`;
}

function bindInternalActionForms(id) {
  const assignDeptForm = document.getElementById("assignDeptForm");
  const assignAgentForm = document.getElementById("assignAgentForm");
  const statusForm = document.getElementById("statusForm");

  assignDeptForm?.addEventListener("submit", async event => {
    event.preventDefault();
    try { await api(`/api/complaint/${id}/assign-department`, { method: "PUT", body: formData(assignDeptForm) }); complaintDetailPage(id); }
    catch (err) { alert(err.message, true); }
  });
  assignAgentForm?.addEventListener("submit", async event => {
    event.preventDefault();
    try { await api(`/api/complaint/${id}/assign-agent/${formData(assignAgentForm).agentId}`, { method: "PUT" }); complaintDetailPage(id); }
    catch (err) { alert(err.message, true); }
  });
  statusForm?.addEventListener("submit", async event => {
    event.preventDefault();
    try { await api(`/api/complaint/${id}/status`, { method: "PUT", body: formData(statusForm) }); complaintDetailPage(id); }
    catch (err) { alert(err.message, true); }
  });
}

async function feedbackPage(id) {
  requireUser();
  setTitle("Feedback", "Rate the resolution experience");
  const existing = await api(`/api/complaint/${id}/feedback`).catch(() => null);
  app.innerHTML = `<div class="panel">${existing ? `<div class="empty">Feedback already recorded: ${existing.rating}/5<br>${escapeHtml(existing.comments || "")}</div>` : `<form id="feedbackForm" class="form-grid">
    <label>Rating<select name="rating">${[1,2,3,4,5].map(n => `<option value="${n}">${n}</option>`).join("")}</select></label>
    <label class="wide">Comments<textarea name="comments"></textarea></label>
    <div class="wide"><button class="button">Submit feedback</button></div>
  </form>`}</div>`;
  feedbackForm?.addEventListener("submit", async event => {
    event.preventDefault();
    try {
      await api(`/api/complaint/${id}/feedback`, { method: "POST", body: formData(feedbackForm) });
      navigate(`/complaints/${id}`);
    } catch (err) { alert(err.message, true); }
  });
}

async function dashboardPage() {
  requireUser("internal");
  setTitle("Dashboard", "Operational summary");
  const summary = await api("/api/dashboard/summary");
  app.innerHTML = `<div class="grid three">
    ${metric("SLA breaches", summary.slaBreachCount)}
    ${metric("Escalations", summary.escalationCount)}
    ${metric("Avg resolution hrs", Number(summary.averageResolutionHours || 0).toFixed(1))}
  </div>
  <div class="grid two" style="margin-top:16px">
    <div class="panel"><h2>By status</h2>${table(["Status", "Count"], (summary.complaintsByStatus || []).map(x => [x.name, x.count]))}</div>
    <div class="panel"><h2>By department</h2>${table(["Department", "Count"], (summary.complaintsByDepartment || []).map(x => [x.name, x.count]))}</div>
  </div>`;
}

async function reportsPage() {
  requireUser("internal");
  setTitle("Reports", "Complaint, department, agent and location reports");
  const [complaints, departments, agents, locations] = await Promise.all([
    api("/api/reports/complaints?page=1&pageSize=20"),
    api("/api/reports/departments"),
    api("/api/reports/agents"),
    api("/api/reports/locations")
  ]);
  app.innerHTML = `<div class="grid">
    <div class="panel"><div class="row between"><h2>Complaints</h2><button id="exportCsv" class="button secondary">Export CSV</button></div>${table(["Ref", "Title", "Status", "Department", "SLA"], (complaints.items || []).map(c => [c.refNumber, c.title, statusBadge(c.status), c.department || "", c.isSlaBreached ? "Breached" : "OK"]))}</div>
    <div class="panel"><h2>Department performance</h2>${table(["Department", "Assigned", "Resolved", "Avg hrs", "SLA breach rate"], departments.map(d => [d.department, d.totalAssigned, d.resolvedCount, Number(d.averageResolutionHours || 0).toFixed(1), `${Number(d.slaBreachRate || 0).toFixed(1)}%`]))}</div>
    <div class="panel"><h2>Agent performance</h2>${table(["Agent", "Handled", "Avg hrs", "Feedback"], agents.map(a => [a.agentName, a.complaintsHandled, Number(a.averageResolutionHours || 0).toFixed(1), Number(a.averageFeedbackRating || 0).toFixed(1)]))}</div>
    <div class="panel"><h2>Location report</h2>${table(["District", "Constituency", "Area", "Block", "Count"], locations.map(l => [l.district, l.constituency, l.area, l.block, l.count]))}</div>
  </div>`;
  exportCsv.onclick = async () => {
    const csv = await api("/api/reports/complaints/export");
    const url = URL.createObjectURL(new Blob([csv], { type: "text/csv" }));
    const link = Object.assign(document.createElement("a"), { href: url, download: "complaints-report.csv" });
    link.click();
    URL.revokeObjectURL(url);
  };
}

async function adminPage() {
  requireUser("internal");
  setTitle("Admin", "Manage users, departments, categories, SLA policies and locations");
  const [users, departments, categories, policies, districts, constituencies, areas, blocks] = await Promise.all([
    api("/api/admin/users"), api("/api/admin/departments"), api("/api/admin/categories"), api("/api/admin/sla-policies"),
    api("/api/admin/locations/districts"), api("/api/admin/locations/constituencies"), api("/api/admin/locations/areas"), api("/api/admin/locations/blocks")
  ]);
  app.innerHTML = `<div class="grid">
    ${adminSection("Users", "users", userForm(departments), table(["Name", "Email", "Role", "Active", ""], users.map(u => [u.fullName, u.email, u.role, u.isActive ? "Yes" : "No", adminActions("users", u)])))}
    ${adminSection("Departments", "departments", textForm(["name", "code", "description"]), table(["Name", "Code", "Active", ""], departments.map(d => [d.name, d.code, d.isActive ? "Yes" : "No", adminActions("departments", d)])))}
    ${adminSection("Categories", "categories", categoryForm(departments), table(["Name", "Department", "Active", ""], categories.map(c => [c.name, c.departmentName || c.department, c.isActive ? "Yes" : "No", adminActions("categories", c)])))}
    ${adminSection("SLA Policies", "sla-policies", slaForm(categories), table(["Category", "Response", "Resolution", "L1", "L2", "L3", ""], policies.map(p => [p.categoryName, p.responseHours, p.resolutionHours, p.escalationLevel1Hours, p.escalationLevel2Hours, p.escalationLevel3Hours, adminActions("sla-policies", p)])))}
    ${adminSection("Locations", "locations/districts", textForm(["name", "code"]), table(["District", "Code", ""], districts.map(d => [d.name, d.code, adminActions("locations/districts", d)])))}
    ${adminSection("Constituencies", "locations/constituencies", locationChildForm("districtId", districts), table(["Constituency", "Code", "District", ""], constituencies.map(c => [c.name, c.code, c.districtId, adminActions("locations/constituencies", c)])))}
    ${adminSection("Areas", "locations/areas", locationChildForm("constituencyId", constituencies), table(["Area", "Code", "Constituency", ""], areas.map(a => [a.name, a.code, a.constituencyId, adminActions("locations/areas", a)])))}
    ${adminSection("Blocks", "locations/blocks", locationChildForm("areaId", areas), table(["Block", "Code", "Area", ""], blocks.map(b => [b.name, b.code, b.areaId, adminActions("locations/blocks", b)])))}
  </div>`;
  document.querySelectorAll("[data-admin-form]").forEach(form => {
    form.addEventListener("submit", async event => {
      event.preventDefault();
      try {
        await api(`/api/admin/${form.dataset.adminForm}`, { method: "POST", body: formData(form) });
        alert("Saved.");
        adminPage();
      } catch (err) { alert(err.message, true); }
    });
  });
  document.querySelectorAll("[data-admin-edit]").forEach(button => {
    button.addEventListener("click", async () => {
      const current = JSON.parse(decodeURIComponent(button.dataset.payload));
      const edited = prompt("Edit JSON and press OK to save", JSON.stringify(stripDisplayFields(current), null, 2));
      if (!edited) return;
      try {
        await api(`/api/admin/${button.dataset.adminEdit}/${current.id}`, { method: "PUT", body: JSON.parse(edited) });
        alert("Updated.");
        adminPage();
      } catch (err) { alert(err.message, true); }
    });
  });
  document.querySelectorAll("[data-admin-delete]").forEach(button => {
    button.addEventListener("click", async () => {
      if (!confirm("Deactivate/delete this record?")) return;
      try {
        await api(`/api/admin/${button.dataset.adminDelete}/${button.dataset.id}`, { method: "DELETE" });
        alert("Updated.");
        adminPage();
      } catch (err) { alert(err.message, true); }
    });
  });
}

function adminSection(title, endpoint, formHtml, tableHtml) {
  return `<div class="panel"><h2>${title}</h2><form class="form-grid" data-admin-form="${endpoint}">${formHtml}<div class="wide"><button class="button">Add</button></div></form><div style="margin-top:14px">${tableHtml}</div></div>`;
}

function adminActions(endpoint, item) {
  const payload = encodeURIComponent(JSON.stringify(item));
  return `<div class="row"><button class="button secondary" data-admin-edit="${endpoint}" data-payload="${payload}">Edit</button><button class="button danger" data-admin-delete="${endpoint}" data-id="${escapeHtml(item.id)}">Delete</button></div>`;
}

function stripDisplayFields(item) {
  const copy = { ...item };
  delete copy.id;
  delete copy.departmentName;
  delete copy.categoryName;
  delete copy.createdAt;
  if (typeof copy.role === "string") copy.role = roleValues.indexOf(copy.role);
  return copy;
}

function userForm(departments) {
  return `<label>Name<input name="fullName" required></label><label>Email<input name="email" type="email" required></label><label>Password<input name="password" type="password" required></label><label>Role<select name="role">${options(roleValues.map((name, id) => ({ id, name })))}</select></label><label>Department<select name="departmentId">${options(departments)}</select></label>`;
}
function textForm(fields) {
  return fields.map(f => `<label>${f}<input name="${f}" ${f === "description" ? "" : "required"}></label>`).join("");
}
function categoryForm(departments) {
  return `${textForm(["name", "description"])}<label>Department<select name="departmentId" required>${options(departments)}</select></label>`;
}
function slaForm(categories) {
  return `<label>Category<select name="categoryId" required>${options(categories)}</select></label>${["responseHours", "resolutionHours", "escalationLevel1Hours", "escalationLevel2Hours", "escalationLevel3Hours"].map(f => `<label>${f}<input name="${f}" type="number" min="1" required></label>`).join("")}`;
}
function locationChildForm(parentKey, parents) {
  return `${textForm(["name", "code"])}<label>${parentKey}<select name="${parentKey}" required>${options(parents)}</select></label>`;
}

function messageCard(m) {
  const mine = String(m.senderId || "") === String(state.user?.id || "");
  return `<div class="message ${mine ? "mine" : ""}"><small>${escapeHtml(senderValues[m.senderType] || m.senderType)} | ${formatDate(m.createdAt)}</small>${escapeHtml(m.message || "")}</div>`;
}
function metric(label, value) {
  return `<div class="panel metric"><span class="muted">${label}</span><strong>${escapeHtml(value)}</strong></div>`;
}
function table(headers, rows) {
  if (!rows.length) return `<div class="empty">No records found.</div>`;
  return `<div class="table-wrap"><table><thead><tr>${headers.map(h => `<th>${escapeHtml(h)}</th>`).join("")}</tr></thead><tbody>${rows.map(row => `<tr>${row.map(cell => `<td>${cell ?? ""}</td>`).join("")}</tr>`).join("")}</tbody></table></div>`;
}
function formatDate(value) {
  return value ? new Date(value).toLocaleString() : "";
}
function requireUser(type) {
  if (!state.user) throw Object.assign(new Error("Login required"), { redirect: "/login" });
  if (type && state.user.userType !== type) throw Object.assign(new Error("Access denied"), { redirect: state.user.userType === "internal" ? "/dashboard" : "/complaints" });
}

async function render() {
  layoutNav();
  document.body.classList.remove("nav-open");
  const path = location.pathname;
  try {
    if (path === "/" || path === "/login") await loginPage();
    else if (path === "/register") await registerPage();
    else if (path === "/dashboard") await dashboardPage();
    else if (path === "/complaints/new") await newComplaintPage();
    else if (path === "/complaints") await complaintsPage();
    else if (path.match(/^\/complaints\/[^/]+\/feedback$/)) await feedbackPage(path.split("/")[2]);
    else if (path.match(/^\/complaints\/[^/]+$/)) await complaintDetailPage(path.split("/")[2]);
    else if (path === "/reports") await reportsPage();
    else if (path === "/admin") await adminPage();
    else navigate("/login");
  } catch (err) {
    if (err.redirect) return navigate(err.redirect);
    app.innerHTML = `<div class="empty">${escapeHtml(err.message)}</div>`;
    alert(err.message, true);
  }
  layoutNav();
}

document.addEventListener("click", event => {
  const link = event.target.closest("[data-link]");
  if (!link) return;
  event.preventDefault();
  navigate(new URL(link.href).pathname);
});
document.getElementById("menuButton").onclick = () => document.body.classList.toggle("nav-open");
window.addEventListener("popstate", render);
render();
