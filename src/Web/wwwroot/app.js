const apiBaseUrl = window.WEB_TEMPLATE_CONFIG?.apiBaseUrl ?? "http://localhost:5139";
const messageEl = document.getElementById("message");
const webStatusEl = document.getElementById("web-status");
const apiStatusEl = document.getElementById("api-status");

async function setStatus(target, url) {
    try {
        const response = await fetch(url);
        target.textContent = response.ok ? "Healthy" : `HTTP ${response.status}`;
    } catch (error) {
        target.textContent = "Unavailable";
    }
}

document.getElementById("greeting-form").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const name = form.get("name")?.toString() ?? "";
    const url = `${apiBaseUrl}/api/greeting?name=${encodeURIComponent(name)}`;

    messageEl.textContent = "Calling API...";

    try {
        const response = await fetch(url);
        if (!response.ok) {
            messageEl.textContent = `API call failed: HTTP ${response.status}`;
            return;
        }

        const payload = await response.json();
        messageEl.textContent = payload.message ?? "No message returned.";
    } catch (error) {
        messageEl.textContent = "API call failed. Start the API with launch.ps1.";
    }
});

setStatus(webStatusEl, "/health");
setStatus(apiStatusEl, `${apiBaseUrl}/health`);
