// JavaScript para el funcionamiento automático y manual del slider
document.addEventListener("DOMContentLoaded", function () {
  const slides = document.querySelectorAll(".slide");
  let currentSlide = 0;
  let slideInterval;

  // Botones de navegación del slider
  const prevBtn = document.querySelector(".prev-btn");
  const nextBtn = document.querySelector(".next-btn");

  function showSlide(n) {
    // Asegurarse de que n esté dentro del rango válido
    if (n >= slides.length) {
      currentSlide = 0;
    } else if (n < 0) {
      currentSlide = slides.length - 1;
    } else {
      currentSlide = n;
    }

    // Ocultar todos los slides y mostrar solo el actual
    slides.forEach((slide) => slide.classList.remove("active"));
    slides[currentSlide].classList.add("active");
  }

  function nextSlide() {
    showSlide(currentSlide + 1);
  }

  function prevSlide() {
    showSlide(currentSlide - 1);
  }

  // Iniciar el intervalo de cambio automático
  function startSlideInterval() {
    slideInterval = setInterval(nextSlide, 5000);
  }

  // Detener el intervalo cuando el usuario interactúa con los botones
  function resetSlideInterval() {
    clearInterval(slideInterval);
    startSlideInterval();
  }

  // Eventos para los botones de navegación
  if (prevBtn && nextBtn) {
    prevBtn.addEventListener("click", function () {
      prevSlide();
      resetSlideInterval();
    });

    nextBtn.addEventListener("click", function () {
      nextSlide();
      resetSlideInterval();
    });
  }

  // Iniciar el slider automático
  startSlideInterval();
});
