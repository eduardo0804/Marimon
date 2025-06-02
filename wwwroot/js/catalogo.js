// Unificar todos los eventos DOMContentLoaded en uno solo
document.addEventListener("DOMContentLoaded", function () {
  // === Código del autocompletado ===
  const searchInput = document.getElementById("autocomplete-search");
  const resultsContainer = document.getElementById("autocomplete-results");
  let timeoutId;

  if (searchInput && resultsContainer) {
    searchInput.addEventListener("input", function () {
      clearTimeout(timeoutId);
      const query = this.value.trim();

      if (query.length < 2) {
        resultsContainer.innerHTML = "";
        resultsContainer.style.display = "none";
        return;
      }

      timeoutId = setTimeout(() => {
        resultsContainer.innerHTML =
          '<div class="p-3 text-center">Buscando...</div>';
        resultsContainer.style.display = "block";

        fetch(`/Catalogo/Autocomplete?query=${encodeURIComponent(query)}`)
          .then((response) =>
            response.ok
              ? response.json()
              : Promise.reject("Error en la búsqueda")
          )
          .then((data) => {
            resultsContainer.innerHTML = "";

            if (data.length === 0) {
              resultsContainer.innerHTML =
                '<div class="p-3 text-center">No se encontraron resultados</div>';
              setTimeout(() => (resultsContainer.style.display = "none"), 1500);
              return;
            }

            data.forEach((item) => {
              const div = document.createElement("div");
              div.className = "autocomplete-item";
              div.style.cssText =
                "display:flex;align-items:center;gap:10px;padding:10px;cursor:pointer";

              // Imagen
              if (item.imagen) {
                const img = document.createElement("img");
                img.src = item.imagen;
                img.alt = item.nombre || "Producto";
                img.style.cssText = "width:40px;height:40px;object-fit:contain";
                div.appendChild(img);
              }

              // Contenedor de texto
              const textContainer = document.createElement("div");
              textContainer.style.flex = "1";

              const nombre = document.createElement("div");
              nombre.textContent = item.nombre || "Sin nombre";
              nombre.style.fontWeight = "bold";
              textContainer.appendChild(nombre);

              if (item.precio !== undefined) {
                const precio = document.createElement("div");
                precio.textContent = `S/ ${parseFloat(item.precio).toFixed(2)}`;
                precio.style.cssText = "color:#E42229;font-size:0.9em";
                textContainer.appendChild(precio);
              }

              div.appendChild(textContainer);

              div.addEventListener("click", () => {
                searchInput.value = item.nombre;
                resultsContainer.style.display = "none";

                // Guardar en historial cuando se selecciona
                guardarEnHistorial(item.nombre);

                // Cargar detalle del producto directamente en modal
                cargarDetalleAutoparte(item.id);
              });
              resultsContainer.appendChild(div);
            });

            resultsContainer.style.display = "block";
          })
          .catch((error) => {
            console.error("Error en autocompletado:", error);
            resultsContainer.innerHTML =
              '<div class="p-3 text-center text-danger">Error de conexión</div>';
            setTimeout(() => (resultsContainer.style.display = "none"), 1500);
          });
      }, 300);
    });

    // Ocultar resultados al hacer clic fuera
    document.addEventListener("click", function (e) {
      const modalAutoparte = document.getElementById("modalAutoparte");
      if (
        e.target !== searchInput &&
        !resultsContainer.contains(e.target) &&
        !(modalAutoparte && modalAutoparte.contains(e.target))
      ) {
        resultsContainer.style.display = "none";
      }
    });

    // Manejo del formulario de búsqueda
    if (searchInput.form) {
      searchInput.form.addEventListener("submit", function (e) {
        e.preventDefault();
        const searchTerm = searchInput.value.trim();
        if (searchTerm) {
          guardarEnHistorial(searchTerm);
          window.location.href = `/Catalogo/Index?buscar=${encodeURIComponent(
            searchTerm
          )}`;
        } else {
          window.location.href = "/Catalogo/Index";
        }
      });
    }
  }

  // === Código del historial de búsquedas ===
  // Obtener historial almacenado o inicializar vacío
  let historialBusquedas =
    JSON.parse(localStorage.getItem("historialBusquedas")) || [];
  actualizarHistorialUI();

  // Función para guardar en historial (para reutilizar)
  function guardarEnHistorial(searchTerm) {
    if (searchTerm) {
      console.log("Guardando búsqueda:", searchTerm); // Depuración

      // Evitar duplicados
      historialBusquedas = historialBusquedas.filter(
        (item) => item !== searchTerm
      );

      // Añadir al principio
      historialBusquedas.unshift(searchTerm);

      // Mantener solo las 10 búsquedas más recientes
      if (historialBusquedas.length > 10) {
        historialBusquedas.pop();
      }

      // Guardar en localStorage
      localStorage.setItem(
        "historialBusquedas",
        JSON.stringify(historialBusquedas)
      );

      // Actualizar la UI inmediatamente
      actualizarHistorialUI();
    }
  }

  // Función para actualizar la UI del historial
  function actualizarHistorialUI() {
    const historialElement = document.getElementById("historial-busquedas");
    if (!historialElement) return;

    if (!historialBusquedas || historialBusquedas.length === 0) {
      historialElement.innerHTML =
        '<li><a class="dropdown-item text-center" href="#">No hay búsquedas recientes</a></li>';
      return;
    }

    historialElement.innerHTML = "";
    historialBusquedas.forEach((termino) => {
      const li = document.createElement("li");
      const a = document.createElement("a");
      a.classList.add("dropdown-item");
      a.href = "/Catalogo/Index?buscar=" + encodeURIComponent(termino);
      a.textContent = termino;

      li.appendChild(a);
      historialElement.appendChild(li);
    });

    // Añadir opción para limpiar historial
    if (historialBusquedas.length > 0) {
      const separador = document.createElement("li");
      separador.innerHTML = '<hr class="dropdown-divider">';
      historialElement.appendChild(separador);

      const limpiarLi = document.createElement("li");
      const limpiarA = document.createElement("a");
      limpiarA.classList.add("dropdown-item");
      limpiarA.href = "#";
      limpiarA.style.color = "#e42229"; // Rojo
      limpiarA.style.fontWeight = "600";
      limpiarA.textContent = "Limpiar historial";
      limpiarA.addEventListener("click", function (e) {
        e.preventDefault();
        localStorage.removeItem("historialBusquedas");
        historialBusquedas = [];
        actualizarHistorialUI();
      });

      limpiarLi.appendChild(limpiarA);
      historialElement.appendChild(limpiarLi);
    }
  }

  // === Código de filtros y categorías ===
  // Ver más/menos categorías
  const showMoreBtn = document.getElementById("showMoreBtn");
  const hiddenCategoriesDiv = document.querySelector(".hidden-categories");

  if (showMoreBtn && hiddenCategoriesDiv) {
    hiddenCategoriesDiv.style.display = "none";

    showMoreBtn.addEventListener("click", function () {
      if (hiddenCategoriesDiv.style.display === "none") {
        hiddenCategoriesDiv.style.display = "block";
        showMoreBtn.innerHTML =
          'Ver menos categorías <i class="fas fa-chevron-up"></i>';
      } else {
        hiddenCategoriesDiv.style.display = "none";
        showMoreBtn.innerHTML =
          'Ver más categorías <i class="fas fa-chevron-down"></i>';
      }
    });
  }

  // Filtros
  const btnAplicarFiltros = document.getElementById("btnAplicarFiltros");
  if (btnAplicarFiltros) {
    btnAplicarFiltros.addEventListener("click", () => aplicarFiltros());
  }

  // Paginación
  document.querySelectorAll(".paginacion").forEach((link) => {
    link.addEventListener("click", function (e) {
      e.preventDefault();
      aplicarFiltros(this.getAttribute("data-pagina"));
    });
  });

  // Inicializar filtros desde URL
  inicializarFiltrosDesdeURL();
  const dropdownToggle = document.getElementById("historialBusqueda");
  if (dropdownToggle) {
    dropdownToggle.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();

      const dropdownMenu = this.nextElementSibling;
      const isOpen = dropdownMenu.classList.contains("show");

      // Cerrar todos los dropdowns abiertos
      document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
        menu.classList.remove('show');
      });

      // Abrir/cerrar el dropdown actual
      if (!isOpen) {
        dropdownMenu.classList.add("show");
      }
    });

    // Cerrar dropdown al hacer clic fuera
    document.addEventListener("click", function (e) {
      if (!dropdownToggle.contains(e.target)) {
        document.getElementById("historial-busquedas").classList.remove("show");
      }
    });
  }
});

// === Funciones de modal ===
function cargarDetalleAutoparte(id) {
  const contenidoModal = document.getElementById("contenidoModal");
  if (!contenidoModal) return;

  contenidoModal.innerHTML =
    '<button class="cerrar-modal" onclick="cerrarModal()">×</button><div class="text-center p-5"><div class="spinner-border" role="status"></div><p class="mt-2">Cargando detalles...</p></div>';
  document.getElementById("modalAutoparte").style.display = "block";

  fetch(`/Catalogo/DetalleAutoparte/${id}`)
    .then((response) =>
      response.ok ? response.text() : Promise.reject("Error en la carga")
    )
    .then((html) => {
      contenidoModal.innerHTML =
        '<button class="cerrar-modal" onclick="cerrarModal()">×</button>' +
        html;

      setTimeout(() => {
        // Inicializar todos los componentes
        inicializarFormularioResenia();
        inicializarSistemaEstrellas();
        inicializarZoom();

        // Asegurar que los botones de eliminar estén visibles desde el inicio
        const resenias = document.querySelectorAll(".resenia-item");
        resenias.forEach((item, index) => {
          // Hacer visible la reseña
          item.style.opacity = "1";
        });
      }, 200);

      const zoomModal = document.getElementById("zoomModal");
      if (zoomModal) zoomModal.style.display = "none";
    })
    .catch((error) => {
      console.error("Error:", error);
      contenidoModal.innerHTML =
        '<button class="cerrar-modal" onclick="cerrarModal()">×</button><div class="alert alert-danger">Error al cargar los detalles. Intente nuevamente.</div>';
    });
}

function abrirZoomModal(imagenSrc) {
  const zoomModal = document.getElementById("zoomModal");
  const zoomedImage = document.getElementById("zoomedImage");
  if (!zoomModal || !zoomedImage) return;

  zoomModal.style.display = "flex";
  zoomedImage.src = imagenSrc;
  zoomedImage.style.transform = "scale(1)";
  let zoomActivo = false;

  // Limpiar eventos anteriores
  zoomedImage.onmousemove = null;
  zoomedImage.onmousedown = null;
  zoomedImage.onmouseleave = null;

  zoomedImage.onmousemove = function (e) {
    if (!zoomActivo) return;
    const rect = zoomedImage.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;
    zoomedImage.style.transformOrigin = `${x}% ${y}%`;
  };

  zoomedImage.onmousedown = function () {
    if (!zoomActivo) {
      zoomedImage.style.transform = "scale(2)";
      zoomActivo = true;
    } else {
      zoomedImage.style.transform = "scale(1)";
      zoomActivo = false;
    }
  };

  zoomedImage.onmouseleave = function () {
    zoomedImage.style.transform = "scale(1)";
    zoomActivo = false;
  };
}

function cerrarModal() {
  const modalAutoparte = document.getElementById("modalAutoparte");
  if (modalAutoparte) modalAutoparte.style.display = "none";

  const zoomModal = document.getElementById("zoomModal");
  if (zoomModal) {
    zoomModal.style.display = "none";
    const zoomedImage = document.getElementById("zoomedImage");
    if (zoomedImage) zoomedImage.style.transform = "scale(1)";
  }
}

function cerrarZoom() {
  const zoomModal = document.getElementById("zoomModal");
  const zoomedImage = document.getElementById("zoomedImage");
  if (zoomModal && zoomedImage) {
    zoomModal.style.display = "none";
    zoomedImage.style.transform = "scale(1)";
  }
}

function inicializarZoom() {
  // Esta función se puede usar para configurar eventos de zoom adicionales si es necesario
  console.log("Zoom inicializado");
}

// === Funciones de reseñas ===
function inicializarSistemaEstrellas() {
  const stars = document.querySelectorAll(".star");
  const valoracionInput = document.getElementById("valoracionInput");

  if (!stars.length || !valoracionInput) return;

  stars.forEach((star) => {
    star.addEventListener("mouseover", () =>
      highlightStars(star.getAttribute("data-value"))
    );

    star.addEventListener("click", () => {
      valoracionInput.value = star.getAttribute("data-value");
      highlightStars(valoracionInput.value, true);
    });
  });

  // Resetear estrellas al salir
  const container = document.querySelector(".stars");
  if (container) {
    container.addEventListener("mouseleave", () =>
      highlightStars(valoracionInput.value, true)
    );
  }

  highlightStars(0, true);
}

function highlightStars(count, permanent = false) {
  document.querySelectorAll(".star").forEach((star) => {
    const value = parseInt(star.getAttribute("data-value"));
    const icon = star.querySelector("i");
    const valoracion = parseInt(
      document.getElementById("valoracionInput")?.value || 0
    );

    if (value <= count) {
      icon.className = "fas fa-star";
      star.style.color = "#E42229";
    } else if (!permanent || valoracion < value) {
      icon.className = "far fa-star";
      star.style.color = "#dddddd";
    }
  });
}

function inicializarFormularioResenia() {
  const form = document.getElementById("formReseniaAutoparte");

  if (form && !form.hasAttribute("data-initialized")) {
    form.setAttribute("data-initialized", "true");

    form.addEventListener("submit", function (e) {
      e.preventDefault();

      // Limpiar validaciones anteriores
      limpiarValidaciones();

      // Validación básica
      const comentario = document.getElementById("res_comentario");
      const valoracion = document.getElementById("valoracionInput");
      let isValid = true;

      if (!comentario?.value.trim()) {
        comentario?.classList.add("is-invalid");
        document
          .getElementById("comentarioError")
          ?.style.setProperty("display", "block");
        isValid = false;
      } else {
        // Si el comentario está completo, remover clase de error
        comentario?.classList.remove("is-invalid");
        comentario?.classList.add("is-valid");
      }

      if (valoracion?.value === "0") {
        document
          .getElementById("valoracionError")
          ?.style.setProperty("display", "block");
        isValid = false;
      }

      if (!isValid) return;

      const formData = new FormData(form);
      const autoparteId = formData.get("aut_id");

      fetch(form.action, {
        method: "POST",
        body: formData,
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "text/html",
        },
        redirect: "manual",
      })
        .then((response) => {
          return response.ok
            ? response.text()
            : response.type === "opaqueredirect"
              ? fetch(
                `/Catalogo/ObtenerReseniasAutoparte?aut_id=${autoparteId}`,
                {
                  headers: { "X-Requested-With": "XMLHttpRequest" },
                }
              ).then((r) => r.text())
              : Promise.reject("Error en el envío");
        })
        .then((html) => {
          const container = document.querySelector(".resenias-list");
          if (container) {
            container.innerHTML = html;
            form.reset();
            valoracion.value = "0";
            highlightStars(0, true);

            // Limpiar todas las validaciones después del envío exitoso
            limpiarValidaciones();

            // Aquí es buen lugar para mostrar el toast
            mostrarToastResenia(); // <-- mostrar mensaje tipo toast que diga "Reseña enviada con éxito" o similar

            // Animar nuevas reseñas
            document
              .querySelectorAll(".resenia-item")
              .forEach((item, index) => {
                setTimeout(() => (item.style.opacity = "1"), 100 * index);
              });
          }
        })
        .catch((error) => {
          console.error("Error:", error);
          alert("Ocurrió un error al enviar la reseña.");
        });
    });
  }
}
function mostrarToastResenia(mensaje = '¡Reseña enviada exitosamente!') {
  const toast = document.createElement('div');
  toast.className = 'toast align-items-center text-white bg-success border-0 position-fixed';
  toast.style.cssText = 'bottom: 20px; right: 20px; z-index: 9999; min-width: 250px;';
  toast.setAttribute('role', 'alert');
  toast.setAttribute('aria-live', 'assertive');
  toast.setAttribute('aria-atomic', 'true');

  toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="fas fa-check-circle me-2"></i>${mensaje}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button>
        </div>
    `;

  document.body.appendChild(toast);

  const bsToast = new bootstrap.Toast(toast, { delay: 3000 });
  bsToast.show();

  toast.addEventListener('hidden.bs.toast', () => {
    toast.remove();
  });
}


// Nueva función para limpiar validaciones
function limpiarValidaciones() {
  // Limpiar mensajes de error
  const comentarioError = document.getElementById("comentarioError");
  const valoracionError = document.getElementById("valoracionError");

  if (comentarioError) {
    comentarioError.style.display = "none";
  }

  if (valoracionError) {
    valoracionError.style.display = "none";
  }

  // Limpiar clases de validación del comentario
  const comentario = document.getElementById("res_comentario");
  if (comentario) {
    comentario.classList.remove("is-invalid", "is-valid");
  }
}

// Función para eliminar reseña (agregar esta si no la tienes)
function eliminarResenia(reseniaId, autoparteId) {
  // Animar la reseña que se va a eliminar
  const reseniaElement = document.querySelector(
    `.resenia-item:has(button[onclick*="${reseniaId}"])`
  );
  if (reseniaElement) {
    reseniaElement.style.opacity = "0.5";
    reseniaElement.style.transform = "translateX(-20px)";
  }

  fetch(`/Catalogo/EliminarResenia/${reseniaId}?aut_id=${autoparteId}`, {
    method: "DELETE",
    headers: {
      "X-Requested-With": "XMLHttpRequest",
      "Content-Type": "application/json",
    },
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success) {
        // Resto del código existente...
        if (reseniaElement) {
          reseniaElement.style.transition = "all 0.3s ease";
          reseniaElement.style.transform = "translateX(-100%)";
          reseniaElement.style.opacity = "0";

          setTimeout(() => {
            reseniaElement.remove();
            actualizarContadorResenias();

            const remainingResenias =
              document.querySelectorAll(".resenia-item");
            if (remainingResenias.length === 0) {
              mostrarMensajeSinResenias();
            }
          }, 300);
        }

        mostrarMensajeExito("Reseña eliminada correctamente.");
      } else {
        mostrarMensajeError(data.message || "Error al eliminar la reseña.");

        if (reseniaElement) {
          reseniaElement.style.opacity = "1";
          reseniaElement.style.transform = "translateX(0)";
        }
      }
    })
    .catch((error) => {
      console.error("Error:", error);
      mostrarMensajeError("Error al eliminar la reseña.");

      if (reseniaElement) {
        reseniaElement.style.opacity = "1";
        reseniaElement.style.transform = "translateX(0)";
      }
    });
}
function mostrarMensajeExito(mensaje) {
  const toast = document.createElement('div');
  toast.className = 'toast align-items-center text-white bg-success border-0 position-fixed';
  toast.style.cssText = 'bottom: 20px; right: 20px; z-index: 9999; min-width: 250px;';
  toast.setAttribute('role', 'alert');
  toast.setAttribute('aria-live', 'assertive');
  toast.setAttribute('aria-atomic', 'true');
  toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="fas fa-check-circle me-2"></i> ${mensaje}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button>
        </div>
    `;

  document.body.appendChild(toast);
  const bsToast = new bootstrap.Toast(toast, { delay: 4000 });
  bsToast.show();

  toast.addEventListener('hidden.bs.toast', () => {
    toast.remove();
  });
}
// === Funciones de carrito ===
function añadirAlCarritoAsync(autoparteId, cantidad) {
  const formData = new FormData();
  formData.append("autoparteId", autoparteId);
  formData.append("cantidad", cantidad);

  fetch("/Carrito/AñadirAlCarrito", {
    method: "POST",
    headers: {
      "X-Requested-With": "XMLHttpRequest",
    },
    body: formData,
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error("Error en la respuesta del servidor");
      }
      return response.json();
    })
    .then((data) => {
      if (data.success) {
        mostrarToast();

        // Actualizar y mostrar el sidebar del carrito
        if (typeof actualizarSidebarCarrito === "function") {
          actualizarSidebarCarrito();
        }
      } else {
        console.error("Error:", data.message);
        alert("No se pudo añadir al carrito: " + data.message);
      }
    })
    .catch((error) => {
      console.error("Error:", error);
      alert("Ocurrió un error al añadir el producto al carrito");
    });
}

function mostrarToast() {
  const toastElement = document.getElementById("toastCarrito");
  if (toastElement) {
    var toast = new bootstrap.Toast(toastElement);
    toast.show();
  }
}

function agregarFavorito(icon) {
  if (icon.classList.contains("fa-regular")) {
    icon.classList.remove("fa-regular");
    icon.classList.add("fa-solid");
  } else {
    icon.classList.remove("fa-solid");
    icon.classList.add("fa-regular");
  }
  const toastFavorito = document.getElementById("toastFavorito");
  if (toastFavorito) {
    var toast = new bootstrap.Toast(toastFavorito);
    toast.show();
  }
}
function mostrarMensajeError(mensaje) {
  const toast = document.createElement('div');
  toast.className = 'toast align-items-center text-white bg-danger border-0 position-fixed';
  toast.style.cssText = 'bottom: 20px; right: 20px; z-index: 9999; min-width: 250px;';
  toast.setAttribute('role', 'alert');
  toast.setAttribute('aria-live', 'assertive');
  toast.setAttribute('aria-atomic', 'true');
  toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="fas fa-times-circle me-2"></i> ${mensaje}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button>
        </div>
    `;

  document.body.appendChild(toast);
  const bsToast = new bootstrap.Toast(toast, { delay: 4000 });
  bsToast.show();

  toast.addEventListener('hidden.bs.toast', () => {
    toast.remove();
  });
}
function agregarFavorito(icon, aut_id) {
  fetch('/Catalogo/AgregarFavorito', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Requested-With': 'XMLHttpRequest'
    },
    body: JSON.stringify({ aut_id: aut_id })
  })
    .then(response => response.json())
    .then(data => {
      if (data.success) {
        icon.classList.remove('fa-regular');
        icon.classList.add('fa-solid');
        mostrarMensajeExito(data.message);
      } else {
        mostrarMensajeError(data.message);
      }
    })
    .catch(() => mostrarMensajeError('Error al agregar a favoritos.'));
}

// === Funciones auxiliares para filtros ===
function aplicarFiltros(pagina = 1) {
  const urlParams = new URLSearchParams(window.location.search);
  const busquedaActual = urlParams.get("buscar") || "";

  const categoriasSeleccionadas = Array.from(
    document.querySelectorAll(".filtro-categoria:checked")
  ).map((checkbox) => checkbox.value);

  const filtroOrden = document.getElementById("filtroOrden");
  const ordenSeleccionado = filtroOrden ? filtroOrden.value : "";

  // Construir URL
  let url = "/Catalogo/Index?pagina=" + pagina;

  if (busquedaActual) url += "&buscar=" + encodeURIComponent(busquedaActual);
  if (ordenSeleccionado)
    url += "&orden=" + encodeURIComponent(ordenSeleccionado);

  categoriasSeleccionadas.forEach((catId) => {
    url += "&categorias=" + catId;
  });

  window.location.href = url;
}

function inicializarFiltrosDesdeURL() {
  const urlParams = new URLSearchParams(window.location.search);

  // Orden
  const orden = urlParams.get("orden");
  const filtroOrden = document.getElementById("filtroOrden");
  if (orden && filtroOrden) filtroOrden.value = orden;

  // Categorías
  const categorias = urlParams.getAll("categorias");
  categorias.forEach((catId) => {
    const checkbox = document.getElementById("cat_" + catId);
    if (checkbox) checkbox.checked = true;
  });

  // Mostrar categorías ocultas si hay seleccionadas
  const hiddenCategoriesDiv = document.querySelector(".hidden-categories");
  const showMoreBtn = document.getElementById("showMoreBtn");

  if (hiddenCategoriesDiv && showMoreBtn) {
    const tieneCategoriasOcultasSeleccionadas = categorias.some((catId) => {
      const checkbox = document.getElementById("cat_" + catId);
      return checkbox && checkbox.closest(".hidden-categories");
    });

    if (
      tieneCategoriasOcultasSeleccionadas &&
      hiddenCategoriesDiv.style.display === "none"
    ) {
      hiddenCategoriesDiv.style.display = "block";
      showMoreBtn.innerHTML =
        'Ver menos categorías <i class="fas fa-chevron-up"></i>';
    }
  }
}

// Variables globales para el modal de confirmación
let reseniaIdAEliminar = null;
let autoparteIdAEliminar = null;

// Función para mostrar el modal de confirmación
function mostrarModalConfirmacion(reseniaId, autoparteId) {
  reseniaIdAEliminar = reseniaId;
  autoparteIdAEliminar = autoparteId;

  const modal = document.getElementById("modalConfirmarEliminar");
  if (modal) {
    modal.style.display = "flex";
    modal.style.alignItems = "center";
    modal.style.justifyContent = "center";
    modal.style.position = "fixed";
    modal.style.top = "0";
    modal.style.left = "0";
    modal.style.width = "100%";
    modal.style.height = "100%";
    modal.style.backgroundColor = "rgba(0, 0, 0, 0.5)";
    modal.style.zIndex = "9999";

    // Agregar animación de entrada
    const modalContent = modal.querySelector(".modal-content");
    if (modalContent) {
      modalContent.style.transform = "scale(0.7)";
      modalContent.style.opacity = "0";
      modalContent.style.transition = "all 0.3s ease";

      setTimeout(() => {
        modalContent.style.transform = "scale(1)";
        modalContent.style.opacity = "1";
      }, 10);
    }
  }
}

// Función para cerrar el modal de confirmación
function cerrarModalConfirmacion() {
  const modal = document.getElementById("modalConfirmarEliminar");
  if (modal) {
    const modalContent = modal.querySelector(".modal-content");

    if (modalContent) {
      modalContent.style.transform = "scale(0.7)";
      modalContent.style.opacity = "0";

      setTimeout(() => {
        modal.style.display = "none";
        reseniaIdAEliminar = null;
        autoparteIdAEliminar = null;
      }, 300);
    } else {
      modal.style.display = "none";
      reseniaIdAEliminar = null;
      autoparteIdAEliminar = null;
    }
  }
}

// Función para confirmar la eliminación
function confirmarEliminacion() {
  if (reseniaIdAEliminar && autoparteIdAEliminar) {
    cerrarModalConfirmacion();
    eliminarResenia(reseniaIdAEliminar, autoparteIdAEliminar);
  }
}

// Cerrar modal al hacer clic fuera de él
document.addEventListener("click", function (event) {
  const modal = document.getElementById("modalConfirmarEliminar");
  if (modal && event.target === modal) {
    cerrarModalConfirmacion();
  }
});

// Cerrar modal con tecla Escape
document.addEventListener("keydown", function (event) {
  if (event.key === "Escape") {
    const modal = document.getElementById("modalConfirmarEliminar");
    if (modal && modal.style.display !== "none") {
      cerrarModalConfirmacion();
    }
  }
});