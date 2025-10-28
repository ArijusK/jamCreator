window.chatHelpers = {
  get: (k) => window.localStorage.getItem(k),
  set: (k, v) => window.localStorage.setItem(k, v),
  focus: (id) => document.getElementById(id)?.focus(),
  scrollToBottom: (id) => {
    const el = document.getElementById(id);
    if (el) el.scrollTop = el.scrollHeight;
  }
};
