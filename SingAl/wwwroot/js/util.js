function postJson(url, body) {
    return fetch(url, { headers: { "Content-Type": "application/json; charset=utf-8" }, method: 'POST', body: JSON.stringify(body) });
}
function max(a, b) {
    return a > b ? a : b;
}
function min(a, b) {
    a < b ? a : b;
}

function lerp(start, end, amt) {
    return (1 - amt) * start + amt * end
}