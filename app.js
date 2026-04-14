const API = `${window.location.origin}/api`;
const incidentTypes = [
  { id: 1, name: "Theft" },
  { id: 2, name: "Assault" },
  { id: 3, name: "Vandalism" },
  { id: 4, name: "Burglary" },
  { id: 5, name: "Traffic Incident" },
  { id: 6, name: "Disturbance" },
  { id: 7, name: "Fraud" }
];
const incidentStatuses = [
  { id: 1, name: "Open", color: "#EF4444" },
  { id: 2, name: "Under Investigation", color: "#F59E0B" },
  { id: 3, name: "Closed", color: "#10B981" },
  { id: 4, name: "Pending Review", color: "#6366F1" }
];

const state = {
  token: localStorage.getItem("pp_token") || "",
  username: localStorage.getItem("pp_user") || "ADMIN",
  incidentPage: 1,
  incidentLimit: 25,
  incidentTotal: 0,
  activeView: "incidents",
  includeArchived: false,
  officerFilter: "active",
  confirmAction: null,
  references: {
    officers: [],
    locations: []
  }
};

function byId(id) {
  return document.getElementById(id);
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function fmtDate(dateStr) {
  if (!dateStr) return "-";
  return new Date(dateStr).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric"
  });
}

function toDateTimeLocalValue(dateStr) {
  if (!dateStr) return "";
  const date = new Date(dateStr);
  const tzOffset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - tzOffset).toISOString().slice(0, 16);
}

function toast(message, color = "var(--accent)") {
  const el = byId("toast");
  el.style.borderColor = color;
  el.style.color = color;
  el.textContent = message;
  el.classList.remove("hidden");
  clearTimeout(el._timer);
  el._timer = setTimeout(() => el.classList.add("hidden"), 3000);
}

async function apiFetch(path, options = {}) {
  const headers = {
    Authorization: `Bearer ${state.token}`,
    ...(options.body ? { "Content-Type": "application/json" } : {}),
    ...(options.headers || {})
  };

  try {
    const response = await fetch(`${API}${path}`, {
      ...options,
      headers
    });

    if (response.status === 401) {
      logout();
      return null;
    }

    return response;
  } catch {
    toast("Network request failed.", "var(--danger)");
    return null;
  }
}

function setAuthenticatedUser(username) {
  state.username = username.toUpperCase();
  localStorage.setItem("pp_user", state.username);
  byId("topbar-user").textContent = `OFFICER: ${state.username}`;
}

async function login() {
  const username = byId("inp-user").value.trim();
  const password = byId("inp-pass").value;
  byId("login-error").textContent = "";

  try {
    const response = await fetch(`${API}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password })
    });

    if (!response.ok) {
      byId("login-error").textContent = "Invalid credentials.";
      return;
    }

    const data = await response.json();
    state.token = data.token;
    localStorage.setItem("pp_token", data.token);
    setAuthenticatedUser(username || "ADMIN");
    await showApp();
  } catch {
    byId("login-error").textContent = "Unable to authenticate.";
  }
}

function logout() {
  state.token = "";
  localStorage.removeItem("pp_token");
  localStorage.removeItem("pp_user");
  byId("app").classList.add("hidden");
  byId("login-screen").classList.remove("hidden");
}

async function showApp() {
  byId("login-screen").classList.add("hidden");
  byId("app").classList.remove("hidden");
  setActiveView(state.activeView);
  await Promise.all([
    loadDashboard(),
    loadReferenceData(),
    loadIncidents(),
    loadOfficers()
  ]);
}

function setActiveView(view) {
  state.activeView = view;
  document.querySelectorAll(".tab-btn").forEach((button) => {
    button.classList.toggle("active", button.dataset.view === view);
  });
  byId("view-incidents").classList.toggle("hidden", view !== "incidents");
  byId("view-officers").classList.toggle("hidden", view !== "officers");
}

async function loadDashboard() {
  const response = await apiFetch("/incidents/dashboard");
  if (!response?.ok) return;

  const stats = await response.json();
  byId("stat-open").textContent = stats.openIncidents;
  byId("stat-inv").textContent = stats.underInvestigation;
  byId("stat-closed").textContent = stats.closedThisMonth;
  byId("stat-total").textContent = stats.totalIncidents;
}

async function loadReferenceData() {
  const [officersResponse, locationsResponse] = await Promise.all([
    apiFetch("/officers"),
    apiFetch("/locations")
  ]);

  if (officersResponse?.ok) {
    state.references.officers = await officersResponse.json();
  }

  if (locationsResponse?.ok) {
    state.references.locations = await locationsResponse.json();
  }
}

function renderEmptyRow(colspan, message) {
  return `<tr><td colspan="${colspan}" class="empty-state">${escapeHtml(message)}</td></tr>`;
}

function renderIncidentRow(incident) {
  const archivedPill = incident.isDeleted
    ? `<span class="pill pill-neutral">ARCHIVED</span>`
    : "";
  const editDisabled = incident.isDeleted ? "disabled" : "";
  const actionButton = incident.isDeleted
    ? `<button class="action-btn" data-action="restore-incident" data-id="${incident.id}">RESTORE</button>`
    : `<button class="action-btn action-btn-danger" data-action="archive-incident" data-id="${incident.id}">ARCHIVE</button>`;

  return `
    <tr class="${incident.isDeleted ? "archived-row" : ""}">
      <td><div class="case-num">${escapeHtml(incident.caseNumber)}</div></td>
      <td><span class="secondary-text">${escapeHtml(incident.incidentType)}</span></td>
      <td>
        <div class="actions-cell">
          <span class="pill" style="color:${escapeHtml(incident.statusColor)};border-color:${escapeHtml(incident.statusColor)}">${escapeHtml(incident.incidentStatus.toUpperCase())}</span>
          ${archivedPill}
        </div>
      </td>
      <td><div class="desc-cell" title="${escapeHtml(incident.description)}">${escapeHtml(incident.description)}</div></td>
      <td>
        <div>${escapeHtml(incident.officerName)}</div>
        <div class="secondary-text">${escapeHtml(incident.badgeNumber)}</div>
      </td>
      <td><div class="desc-cell" title="${escapeHtml(incident.locationAddress)}">${escapeHtml(incident.locationAddress)}</div></td>
      <td>${fmtDate(incident.reportedAt)}</td>
      <td>
        <div class="actions-cell">
          <button class="action-btn" data-action="edit-incident" data-id="${incident.id}" ${editDisabled}>EDIT</button>
          ${actionButton}
        </div>
      </td>
    </tr>
  `;
}

async function loadIncidents() {
  const body = byId("incidents-body");
  const statusId = byId("filter-status").value;
  const typeId = byId("filter-type").value;
  const includeArchived = byId("filter-archived").checked;
  state.includeArchived = includeArchived;

  body.innerHTML = '<tr><td colspan="8"><div class="loading"></div></td></tr>';

  const params = new URLSearchParams({
    page: String(state.incidentPage),
    pageSize: String(state.incidentLimit),
    includeArchived: String(includeArchived)
  });
  if (statusId) params.set("statusId", statusId);
  if (typeId) params.set("typeId", typeId);

  const response = await apiFetch(`/incidents?${params.toString()}`);
  if (!response?.ok) {
    body.innerHTML = renderEmptyRow(8, "Unable to load incidents.");
    return;
  }

  state.incidentTotal = parseInt(response.headers.get("X-Total-Count") || "0", 10);
  const incidents = await response.json();

  body.innerHTML = incidents.length
    ? incidents.map(renderIncidentRow).join("")
    : renderEmptyRow(8, includeArchived ? "No incidents match the current filters." : "No active incidents match the current filters.");

  const start = state.incidentTotal ? (state.incidentPage - 1) * state.incidentLimit + 1 : 0;
  const end = Math.min(state.incidentPage * state.incidentLimit, state.incidentTotal);
  byId("pag-info").textContent = state.incidentTotal
    ? `RECORDS ${start}-${end} OF ${state.incidentTotal}`
    : "NO RECORDS";
  byId("btn-prev").disabled = state.incidentPage <= 1;
  byId("btn-next").disabled = state.incidentPage * state.incidentLimit >= state.incidentTotal;
}

function renderOfficerRow(officer) {
  const statusText = officer.isActive ? "ACTIVE" : "INACTIVE";
  const action = officer.isActive
    ? `<button class="action-btn action-btn-danger" data-action="deactivate-officer" data-id="${officer.id}">DEACTIVATE</button>`
    : `<button class="action-btn" data-action="reactivate-officer" data-id="${officer.id}">REACTIVATE</button>`;

  return `
    <tr>
      <td><div class="case-num">${escapeHtml(officer.badgeNumber)}</div></td>
      <td>
        <div>${escapeHtml(`${officer.firstName} ${officer.lastName}`)}</div>
      </td>
      <td>${escapeHtml(officer.rank)}</td>
      <td>${escapeHtml(officer.precinct)}</td>
      <td><span class="pill ${officer.isActive ? "" : "pill-neutral"}">${statusText}</span></td>
      <td>${officer.incidentCount}</td>
      <td><div class="actions-cell">${action}</div></td>
    </tr>
  `;
}

async function loadOfficers() {
  const body = byId("officers-body");
  const filter = byId("officers-filter").value;
  state.officerFilter = filter;
  body.innerHTML = '<tr><td colspan="7"><div class="loading"></div></td></tr>';

  const query = filter === "all" ? "" : `?activeOnly=${filter === "active"}`;
  const response = await apiFetch(`/officers${query}`);
  if (!response?.ok) {
    body.innerHTML = renderEmptyRow(7, "Unable to load officers.");
    return;
  }

  const officers = await response.json();
  body.innerHTML = officers.length
    ? officers.map(renderOfficerRow).join("")
    : renderEmptyRow(7, "No officers match the current filter.");
}

function fillSelect(select, options, selectedValue, labelSelector) {
  select.innerHTML = options.map((option) => {
    const selected = String(option.id) === String(selectedValue) ? "selected" : "";
    return `<option value="${option.id}" ${selected}>${escapeHtml(labelSelector(option))}</option>`;
  }).join("");
}

async function openIncidentModal(incidentId) {
  await loadReferenceData();
  const response = await apiFetch(`/incidents/${incidentId}?includeArchived=true`);
  if (!response?.ok) {
    toast("Unable to load the selected incident.", "var(--danger)");
    return;
  }

  const incident = await response.json();
  byId("incident-id").value = incident.id;
  byId("incident-modal-case").textContent = incident.caseNumber;
  byId("incident-description").value = incident.description;
  byId("incident-occurred-at").value = toDateTimeLocalValue(incident.occurredAt);
  fillSelect(byId("incident-status-id"), incidentStatuses, incident.incidentStatusId, (item) => item.name);
  fillSelect(byId("incident-type-id"), incidentTypes, incident.incidentTypeId, (item) => item.name);
  fillSelect(byId("incident-officer-id"), state.references.officers, incident.officerId, (item) => `${item.badgeNumber} - ${item.firstName} ${item.lastName}${item.isActive ? "" : " (Inactive)"}`);
  fillSelect(byId("incident-location-id"), state.references.locations, incident.locationId, (item) => `${item.street}, ${item.city}`);
  byId("incident-form-error").textContent = "";
  byId("incident-modal").classList.remove("hidden");
}

function closeIncidentModal() {
  byId("incident-modal").classList.add("hidden");
  byId("incident-form").reset();
  byId("incident-form-error").textContent = "";
}

function openConfirmDialog({ title, message, confirmLabel, onConfirm, danger = false }) {
  byId("confirm-title").textContent = title;
  byId("confirm-message").textContent = message;
  const confirmButton = byId("confirm-action");
  confirmButton.textContent = confirmLabel;
  confirmButton.classList.toggle("btn-danger", danger);
  confirmButton.classList.toggle("btn-primary", !danger);
  state.confirmAction = onConfirm;
  byId("confirm-modal").classList.remove("hidden");
}

function closeConfirmDialog() {
  state.confirmAction = null;
  byId("confirm-modal").classList.add("hidden");
}

async function submitIncidentForm(event) {
  event.preventDefault();
  const incidentId = byId("incident-id").value;
  const payload = {
    description: byId("incident-description").value.trim(),
    occurredAt: byId("incident-occurred-at").value ? new Date(byId("incident-occurred-at").value).toISOString() : null,
    incidentStatusId: Number(byId("incident-status-id").value),
    incidentTypeId: Number(byId("incident-type-id").value),
    officerId: Number(byId("incident-officer-id").value),
    locationId: Number(byId("incident-location-id").value)
  };

  if (payload.description.length < 5) {
    byId("incident-form-error").textContent = "Description must be at least 5 characters.";
    return;
  }

  const response = await apiFetch(`/incidents/${incidentId}`, {
    method: "PUT",
    body: JSON.stringify(payload)
  });

  if (!response?.ok) {
    byId("incident-form-error").textContent = "Unable to save the incident.";
    return;
  }

  closeIncidentModal();
  toast("Incident updated.");
  await Promise.all([loadDashboard(), loadIncidents(), loadOfficers(), loadReferenceData()]);
}

async function archiveIncident(incidentId) {
  const response = await apiFetch(`/incidents/${incidentId}`, { method: "DELETE" });
  if (!response?.ok) {
    toast("Unable to archive the incident.", "var(--danger)");
    return;
  }

  toast("Incident archived.");
  await Promise.all([loadDashboard(), loadIncidents(), loadOfficers()]);
}

async function restoreIncident(incidentId) {
  const response = await apiFetch(`/incidents/${incidentId}/restore`, { method: "POST" });
  if (!response?.ok) {
    toast("Unable to restore the incident.", "var(--danger)");
    return;
  }

  toast("Incident restored.");
  await Promise.all([loadDashboard(), loadIncidents(), loadOfficers()]);
}

async function updateOfficerStatus(officerId, reactivate) {
  const response = await apiFetch(
    reactivate ? `/officers/${officerId}/reactivate` : `/officers/${officerId}`,
    { method: reactivate ? "POST" : "DELETE" }
  );

  if (!response?.ok) {
    toast(`Unable to ${reactivate ? "reactivate" : "deactivate"} officer.`, "var(--danger)");
    return;
  }

  toast(`Officer ${reactivate ? "reactivated" : "deactivated"}.`);
  await Promise.all([loadOfficers(), loadReferenceData()]);
}

async function exportXml() {
  const statusId = byId("filter-status").value;
  const suffix = statusId ? `?statusId=${encodeURIComponent(statusId)}` : "";
  const response = await apiFetch(`/incidents/export/xml${suffix}`);
  if (!response?.ok) {
    toast("Export failed.", "var(--danger)");
    return;
  }

  const xml = await response.text();
  const blob = new Blob([xml], { type: "application/xml" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `incidents-export-${Date.now()}.xml`;
  link.click();
  URL.revokeObjectURL(link.href);
  toast("XML export downloaded.");
}

function wireEvents() {
  byId("btn-login").addEventListener("click", login);
  byId("inp-pass").addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      login();
    }
  });
  byId("btn-logout").addEventListener("click", logout);

  document.querySelectorAll(".tab-btn").forEach((button) => {
    button.addEventListener("click", () => setActiveView(button.dataset.view));
  });

  byId("filter-status").addEventListener("change", () => {
    state.incidentPage = 1;
    loadIncidents();
  });
  byId("filter-type").addEventListener("change", () => {
    state.incidentPage = 1;
    loadIncidents();
  });
  byId("filter-archived").addEventListener("change", () => {
    state.incidentPage = 1;
    loadIncidents();
  });
  byId("btn-refresh").addEventListener("click", () => Promise.all([loadDashboard(), loadIncidents(), loadOfficers()]));
  byId("btn-prev").addEventListener("click", () => {
    state.incidentPage -= 1;
    loadIncidents();
  });
  byId("btn-next").addEventListener("click", () => {
    state.incidentPage += 1;
    loadIncidents();
  });
  byId("btn-export-xml").addEventListener("click", exportXml);

  byId("officers-filter").addEventListener("change", loadOfficers);
  byId("btn-refresh-officers").addEventListener("click", () => Promise.all([loadOfficers(), loadReferenceData()]));

  byId("incidents-body").addEventListener("click", (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) return;
    const incidentId = button.dataset.id;

    if (button.dataset.action === "edit-incident") {
      openIncidentModal(incidentId);
      return;
    }

    if (button.dataset.action === "archive-incident") {
      openConfirmDialog({
        title: "Archive incident?",
        message: "The incident will be hidden from the default list but can still be restored later.",
        confirmLabel: "ARCHIVE",
        danger: true,
        onConfirm: async () => {
          closeConfirmDialog();
          await archiveIncident(incidentId);
        }
      });
      return;
    }

    if (button.dataset.action === "restore-incident") {
      openConfirmDialog({
        title: "Restore incident?",
        message: "The incident will return to the default active records list.",
        confirmLabel: "RESTORE",
        onConfirm: async () => {
          closeConfirmDialog();
          await restoreIncident(incidentId);
        }
      });
    }
  });

  byId("officers-body").addEventListener("click", (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) return;
    const officerId = button.dataset.id;
    const reactivate = button.dataset.action === "reactivate-officer";

    openConfirmDialog({
      title: reactivate ? "Reactivate officer?" : "Deactivate officer?",
      message: reactivate
        ? "The officer will return to active staffing lists."
        : "The officer will stay in the system but be marked inactive.",
      confirmLabel: reactivate ? "REACTIVATE" : "DEACTIVATE",
      danger: !reactivate,
      onConfirm: async () => {
        closeConfirmDialog();
        await updateOfficerStatus(officerId, reactivate);
      }
    });
  });

  byId("incident-form").addEventListener("submit", submitIncidentForm);
  byId("btn-close-incident-modal").addEventListener("click", closeIncidentModal);
  byId("btn-cancel-incident-modal").addEventListener("click", closeIncidentModal);
  byId("btn-cancel-confirm").addEventListener("click", closeConfirmDialog);
  byId("confirm-action").addEventListener("click", async () => {
    if (state.confirmAction) {
      await state.confirmAction();
    }
  });

  byId("incident-modal").addEventListener("click", (event) => {
    if (event.target === byId("incident-modal")) {
      closeIncidentModal();
    }
  });
  byId("confirm-modal").addEventListener("click", (event) => {
    if (event.target === byId("confirm-modal")) {
      closeConfirmDialog();
    }
  });
}

wireEvents();

if (state.token) {
  setAuthenticatedUser(state.username);
  showApp();
}
