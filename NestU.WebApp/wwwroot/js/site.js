// Đánh dấu menu đang mở
document.querySelectorAll('.navbar .nav-link').forEach(a => {
  if (a.getAttribute('href') === window.location.pathname) a.classList.add('active');
});
