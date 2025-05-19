// Función para abrir el modal de detalle de autoparte
function cargarDetalleAutoparte(id) {
    console.log("Abriendo detalle de autoparte:", id);
    fetch(`/Catalogo/DetalleAutoparte/${id}`)
        .then(response => response.text())
        .then(html => {
            document.getElementById("contenidoModal").innerHTML = '<button class="cerrar-modal" onclick="cerrarModal()">×</button>' + html;
            document.getElementById("modalAutoparte").style.display = "block";

            // Inicializar el zoom
            const zoomModal = document.getElementById("zoomModal");
            if (zoomModal) zoomModal.style.display = "none";

            // Inicializar el formulario de reseñas después de cargar el contenido
            setTimeout(function () {
                inicializarFormularioResenia();
                inicializarSistemaEstrellas(); // ¡Importante! Inicializar estrellas aquí también

                // Animar las reseñas
                const reseniaItems = document.querySelectorAll('.resenia-item');
                reseniaItems.forEach((item, index) => {
                    setTimeout(() => {
                        item.style.opacity = '1';
                    }, 100 * index);
                });
            }, 300);
        })
        .catch(error => console.error("Error al cargar el detalle de la autoparte:", error));
}

// Función para cerrar el modal
function cerrarModal() {
    console.log("Cerrando modal...");
    const modal = document.getElementById("modalAutoparte");
    if (modal) modal.style.display = "none";

    // También cerrar el modal de zoom si está abierto
    const zoomModal = document.getElementById("zoomModal");
    if (zoomModal) zoomModal.style.display = "none";
}

// Función para inicializar el sistema de estrellas
function inicializarSistemaEstrellas() {
    console.log("Inicializando sistema de estrellas");
    const stars = document.querySelectorAll('.star');
    const valoracionInput = document.getElementById('valoracionInput');

    if (!stars.length || !valoracionInput) {
        console.log("No se encontraron estrellas o el input de valoración");
        return;
    }

    console.log("Configurando eventos para", stars.length, "estrellas");
    stars.forEach(star => {
        // Evento hover
        star.addEventListener('mouseover', () => {
            const value = star.getAttribute('data-value');
            highlightStars(value);
        });

        // Evento click
        star.addEventListener('click', () => {
            const value = star.getAttribute('data-value');
            valoracionInput.value = value;
            highlightStars(value, true);
        });
    });

    // Resetear estrellas cuando el mouse sale del contenedor
    const starsContainer = document.querySelector('.stars');
    if (starsContainer) {
        starsContainer.addEventListener('mouseleave', () => {
            const selectedValue = valoracionInput.value;
            highlightStars(selectedValue, true);
        });
    }

    // Inicializar estrellas con valor 0
    highlightStars(0, true);
}

// Función para iluminar las estrellas
function highlightStars(count, permanent = false) {
    const stars = document.querySelectorAll('.star');
    if (!stars.length) return;

    const valoracionInput = document.getElementById('valoracionInput');
    if (!valoracionInput) return;

    stars.forEach(star => {
        const starValue = parseInt(star.getAttribute('data-value'));
        const icon = star.querySelector('i');

        if (starValue <= count) {
            icon.className = 'fas fa-star';
            star.style.color = '#E42229';
        } else {
            if (!permanent || parseInt(valoracionInput.value) < starValue) {
                icon.className = 'far fa-star';
                star.style.color = '#dddddd';
            }
        }
    });
}

// Mejorar la función inicializarFormularioResenia
function inicializarFormularioResenia() {
    const form = document.getElementById('formReseniaAutoparte');

    if (form && !form.hasAttribute('data-initialized')) {
        form.setAttribute('data-initialized', 'true');
        console.log("Inicializando formulario de reseña");

        form.addEventListener('submit', function (e) {
            e.preventDefault(); // Evitar el envío tradicional del formulario
            console.log("Formulario enviado via AJAX");

            // Validaciones básicas del formulario
            const comentarioInput = document.getElementById('res_comentario');
            const valoracionInput = document.getElementById('valoracionInput');
            let isValid = true;

            // Validar comentario
            if (!comentarioInput || comentarioInput.value.trim() === '') {
                if (comentarioInput) comentarioInput.classList.add('is-invalid');
                const comentarioError = document.getElementById('comentarioError');
                if (comentarioError) comentarioError.style.display = 'block';
                isValid = false;
            }

            // Validar valoración
            if (!valoracionInput || valoracionInput.value === '0') {
                const valoracionError = document.getElementById('valoracionError');
                if (valoracionError) valoracionError.style.display = 'block';
                isValid = false;
            }

            if (!isValid) {
                console.log("Formulario inválido");
                return;
            }

            const formData = new FormData(form);
            const aut_id = formData.get('aut_id'); // Obtener el ID de la autoparte

            fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'text/html'
                },
                redirect: 'manual'
            })
                .then(response => {
                    if (response.ok) {
                        return response.text();
                    } else if (response.type === 'opaqueredirect') {
                        // Si se intenta redireccionar, mantenerse en la página actual
                        console.log("Detectada redirección, permaneciendo en modal");
                        return fetch(`/Catalogo/ObtenerReseniasAutoparte?aut_id=${aut_id}`, {
                            headers: { 'X-Requested-With': 'XMLHttpRequest' }
                        }).then(r => r.text());
                    } else {
                        throw new Error('Error al enviar la reseña.');
                    }
                })
                .then(html => {
                    console.log("Respuesta recibida, actualizando reseñas");
                    // Actualizar solo la sección de reseñas
                    const reseniasContainer = document.querySelector('.resenias-list');
                    if (reseniasContainer) {
                        reseniasContainer.innerHTML = html;
                        form.reset();
                        // Reinicializar las estrellas a 0
                        if (valoracionInput) valoracionInput.value = '0';
                        highlightStars(0, true);

                        // Animar las nuevas reseñas
                        const reseniaItems = document.querySelectorAll('.resenia-item');
                        reseniaItems.forEach((item, index) => {
                            setTimeout(() => {
                                item.style.opacity = '1';
                            }, 100 * index);
                        });

                        const modal = document.getElementById("modalAutoparte");
                        if (modal) {
                            modal.style.display = "block";

                            const cerrarBtn = document.querySelector('.cerrar-modal');
                            if (cerrarBtn) {
                                cerrarBtn.onclick = cerrarModal;
                            } else {
                                const nuevoBotonCerrar = document.createElement('button');
                                nuevoBotonCerrar.className = 'cerrar-modal';
                                nuevoBotonCerrar.textContent = '×';
                                nuevoBotonCerrar.onclick = cerrarModal;

                                const contenidoModal = document.getElementById('contenidoModal');
                                if (contenidoModal) {
                                    contenidoModal.prepend(nuevoBotonCerrar);
                                }
                            }

                            console.log("Modal mantenido visible");
                        }
                    }
                })
                .catch(error => {
                    console.error("Error en la solicitud AJAX:", error);
                    alert('Ocurrió un error al enviar la reseña.');
                });
        });
    }
}

// Función para eliminar una reseña
function eliminarResenia(reseniaId, autoparteId) {
    if (confirm('¿Estás seguro de eliminar esta reseña?')) {
        console.log(`Eliminando reseña: ${reseniaId} de autoparte: ${autoparteId}`); // Para depuración

        fetch(`/Catalogo/EliminarResenia?id=${reseniaId}&aut_id=${autoparteId}`, {
            method: 'DELETE',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Error en la respuesta del servidor');
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    const reseniaElement = document.getElementById(`resenia-${reseniaId}`);
                    if (reseniaElement) {
                        reseniaElement.remove();
                        alert('Reseña eliminada correctamente');
                    }

                    // Verificar si no quedan reseñas
                    const reseniasContainer = document.querySelector('.resenias-list');
                    if (reseniasContainer && reseniasContainer.children.length === 0) {
                        reseniasContainer.innerHTML = '<p class="text-muted">No hay reseñas para esta autoparte.</p>';
                    }

                } else {
                    alert(data.message || 'Error al eliminar la reseña');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Ocurrió un error al eliminar la reseña');
            });
    }
}

// Añadir al carrito de forma asincrónica
function añadirAlCarritoAsync(autoparteId, cantidad) {
    const formData = new FormData();
    formData.append('autoparteId', autoparteId);
    formData.append('cantidad', cantidad);

    fetch('/Carrito/AñadirAlCarrito', {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: formData
    })
        .then(response => {
            if (!response.ok) throw new Error('Error al añadir al carrito');
            return response.json();
        })
        .then(data => {
            if (data.success) {
                var toast = new bootstrap.Toast(document.getElementById("toastCarrito"));
                toast.show();
            } else {
                alert(data.message || 'Error al añadir al carrito');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Ocurrió un error al añadir el producto al carrito');
        });
}

// Evento DOMContentLoaded para inicializar todo
document.addEventListener('DOMContentLoaded', function () {
    // Código del autocompletado (mantener como está)
    const searchInput = document.getElementById('autocomplete-search');
    const resultsContainer = document.getElementById('autocomplete-results');
    let timeoutId;

    // Implementación del autocompletado
    if (searchInput && resultsContainer) {
        const HISTORIAL_KEY = 'busquedas_recientes';
        const MAX_HISTORIAL = 10;

        // Función para obtener el historial guardado
        function obtenerHistorial() {
            const historial = localStorage.getItem(HISTORIAL_KEY);
            return historial ? JSON.parse(historial) : [];
        }

        // Función para guardar una nueva búsqueda en el historial
        function guardarEnHistorial(busqueda) {
            if (!busqueda.trim()) return;

            let historial = obtenerHistorial();
            // Eliminar si ya existe para evitar duplicados
            historial = historial.filter(item => item.toLowerCase() !== busqueda.toLowerCase());
            // Añadir al principio
            historial.unshift(busqueda);
            if (historial.length > MAX_HISTORIAL) {
                historial = historial.slice(0, MAX_HISTORIAL);
            }

            localStorage.setItem(HISTORIAL_KEY, JSON.stringify(historial));
            actualizarHistorialUI();
        }

        // Actualizar la interfaz del historial
        function actualizarHistorialUI() {
            const historial = obtenerHistorial();
            const historialContainer = document.getElementById('historial-busquedas');

            if (historialContainer) {
                if (historial.length === 0) {
                    historialContainer.innerHTML = '<li><a class="dropdown-item text-center" href="#">No hay búsquedas recientes</a></li>';
                } else {
                    historialContainer.innerHTML = '';
                    historial.forEach(item => {
                        const li = document.createElement('li');
                        const a = document.createElement('a');
                        a.className = 'dropdown-item';
                        a.href = `/Catalogo/Index?buscar=${encodeURIComponent(item)}`;
                        a.textContent = item;
                        li.appendChild(a);
                        historialContainer.appendChild(li);
                    });
                }
            }
        }

        // Cargar el historial al inicio
        actualizarHistorialUI();

        // Gestión del autocompletado
        searchInput.addEventListener('input', function () {
            const query = this.value.trim();

            // Limpiar cualquier timeout previo para implementar debounce
            clearTimeout(timeoutId);

            // Ocultar resultados si el input está vacío
            if (query.length < 2) {
                resultsContainer.style.display = 'none';
                return;
            }

            // Esperar 300ms antes de hacer la búsqueda (debounce)
            timeoutId = setTimeout(() => {
                fetch(`/Catalogo/Autocomplete?query=${encodeURIComponent(query)}`)
                    .then(response => response.json())
                    .then(data => {
                        if (data && data.length > 0) {
                            // Mostrar resultados
                            resultsContainer.innerHTML = '';
                            data.forEach(item => {
                                const div = document.createElement('div');
                                div.className = 'autocomplete-item';

                                // Crear un contenedor flexible para la imagen y el texto
                                div.style.display = 'flex';
                                div.style.alignItems = 'center';
                                div.style.gap = '10px';

                                // Añadir la imagen si existe
                                if (item.imagen) {
                                    const img = document.createElement('img');
                                    img.src = item.imagen;
                                    img.alt = item.nombre || 'Producto';
                                    img.style.width = '40px';
                                    img.style.height = '40px';
                                    img.style.objectFit = 'contain';
                                    div.appendChild(img);
                                }

                                // Contenedor para texto y precio
                                const textContainer = document.createElement('div');
                                textContainer.style.flex = '1';

                                // Añadir el nombre
                                const nombreSpan = document.createElement('div');
                                nombreSpan.textContent = item.nombre || 'Sin nombre';
                                nombreSpan.style.fontWeight = 'bold';
                                textContainer.appendChild(nombreSpan);

                                // Añadir el precio si existe
                                if (item.precio !== undefined) {
                                    const precioSpan = document.createElement('div');
                                    precioSpan.textContent = `S/ ${item.precio.toFixed(2)}`;
                                    precioSpan.style.color = '#E42229';
                                    precioSpan.style.fontSize = '0.9em';
                                    textContainer.appendChild(precioSpan);
                                }

                                div.appendChild(textContainer);

                                // El texto a mostrar en el input cuando se selecciona
                                const textoMostrar = item.nombre || (typeof item === 'string' ? item : JSON.stringify(item));

                                div.addEventListener('click', function () {
                                    searchInput.value = textoMostrar;
                                    resultsContainer.style.display = 'none';
                                    searchInput.form.submit();
                                    // Guardar en historial al seleccionar
                                    guardarEnHistorial(textoMostrar);
                                });

                                resultsContainer.appendChild(div);
                            });
                            resultsContainer.style.display = 'block';
                        } else {
                            resultsContainer.style.display = 'none';
                        }
                    })
                    .catch(error => {
                        console.error('Error en autocompletado:', error);
                        resultsContainer.style.display = 'none';
                    });
            }, 300);
        });

        // Ocultar resultados al hacer clic fuera
        document.addEventListener('click', function (e) {
            if (e.target !== searchInput && e.target !== resultsContainer) {
                resultsContainer.style.display = 'none';
            }
        });

        // Guardar en historial al enviar el formulario
        searchInput.form.addEventListener('submit', function () {
            guardarEnHistorial(searchInput.value.trim());
        });
    }

    // Configuración del botón "Ver más categorías"
    const showMoreBtn = document.getElementById('showMoreBtn');
    const hiddenCategoriesDiv = document.querySelector('.hidden-categories');

    if (showMoreBtn && hiddenCategoriesDiv) {
        // Asegurar que comienza oculto
        hiddenCategoriesDiv.style.display = 'none';

        // Añadir event listener simplificado
        showMoreBtn.addEventListener('click', function () {
            if (hiddenCategoriesDiv.style.display === 'none') {
                hiddenCategoriesDiv.style.display = 'block';
                showMoreBtn.innerHTML = 'Ver menos categorías <i class="fas fa-chevron-up"></i>';
            } else {
                hiddenCategoriesDiv.style.display = 'none';
                showMoreBtn.innerHTML = 'Ver más categorías <i class="fas fa-chevron-down"></i>';
            }
        });
    }

    const btnAplicarFiltros = document.getElementById('btnAplicarFiltros');
    const filtroOrden = document.getElementById('filtroOrden');

    if (btnAplicarFiltros) {
        btnAplicarFiltros.addEventListener('click', function () {
            console.log("Aplicando filtros...");
            aplicarFiltros();
        });
    }

    // También manejar cambios en la paginación
    document.querySelectorAll('.paginacion').forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const pagina = this.getAttribute('data-pagina');
            aplicarFiltros(pagina);
        });
    });

    // Función para aplicar los filtros y navegar a la URL resultante
    function aplicarFiltros(pagina = 1) {
        // Obtener el valor de búsqueda actual (si existe)
        const urlParams = new URLSearchParams(window.location.search);
        const busquedaActual = urlParams.get('buscar') || "";

        // Obtener los filtros seleccionados
        const categoriasSeleccionadas = Array.from(
            document.querySelectorAll('.filtro-categoria:checked')
        ).map(checkbox => checkbox.value);

        const ordenSeleccionado = filtroOrden.value;

        // Construir la URL con los parámetros
        let url = '/Catalogo/Index?pagina=' + pagina;

        if (busquedaActual) {
            url += '&buscar=' + encodeURIComponent(busquedaActual);
        }

        if (ordenSeleccionado) {
            url += '&orden=' + encodeURIComponent(ordenSeleccionado);
        }

        // Añadir categorías seleccionadas
        categoriasSeleccionadas.forEach(catId => {
            url += '&categorias=' + catId;
        });

        console.log("Navegando a:", url);
        window.location.href = url;
    }

    // Función para inicializar los filtros según la URL actual
    function inicializarFiltrosDesdeURL() {
        const urlParams = new URLSearchParams(window.location.search);

        // Inicializar orden
        const orden = urlParams.get('orden');
        if (orden && filtroOrden) {
            filtroOrden.value = orden;
        }

        // Inicializar categorías
        const categorias = urlParams.getAll('categorias');
        categorias.forEach(catId => {
            const checkbox = document.getElementById('cat_' + catId);
            if (checkbox) {
                checkbox.checked = true;
            }
        });

        // Si hay categorías ocultas que están seleccionadas, mostrar la sección
        const hiddenCategoriesDiv = document.querySelector('.hidden-categories');
        const showMoreBtn = document.getElementById('showMoreBtn');

        if (hiddenCategoriesDiv && showMoreBtn) {
            const tieneCategoriasOcultasSeleccionadas = categorias.some(catId => {
                const checkbox = document.getElementById('cat_' + catId);
                return checkbox && checkbox.closest('.hidden-categories');
            });

            if (tieneCategoriasOcultasSeleccionadas && hiddenCategoriesDiv.style.display === 'none') {
                hiddenCategoriesDiv.style.display = 'block';
                showMoreBtn.innerHTML = 'Ver menos categorías <i class="fas fa-chevron-up"></i>';
            }
        }
    }

    // Llamar a la función de inicialización cuando la página carga
    inicializarFiltrosDesdeURL();
});