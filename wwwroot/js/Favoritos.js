document.addEventListener("DOMContentLoaded", function() {
    const searchForm = document.getElementById('formBuscarFavoritos');
    const searchInput = document.getElementById('searchInput');
    
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            e.preventDefault();
            buscarFavoritos(searchInput.value.trim());
        });
    }
    
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            const termino = this.value.trim();
            
            searchTimeout = setTimeout(function() {
                if (termino.length >= 1 || termino.length === 0) {
                    buscarFavoritos(termino);
                }
            }, 500);
        });
    }
});

function buscarFavoritos(termino) {
    const favoritosContainer = document.getElementById('favoritosContainer');
    
    if (favoritosContainer) {
        favoritosContainer.classList.add('loading');
        
        fetch(`?handler=Buscar&termino=${encodeURIComponent(termino)}`, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Error en la respuesta del servidor');
            }
            return response.text();
        })
        .then(html => {
            favoritosContainer.innerHTML = html;
        })
        .catch(error => {
            console.error('Error:', error);
            favoritosContainer.innerHTML = `
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-circle me-2"></i>
                    Error al buscar favoritos. Intente nuevamente.
                </div>
            `;
        })
        .finally(() => {
            favoritosContainer.classList.remove('loading');
        });
    }
}

function eliminarFavorito(autoparteId) {
    const favoritoItem = document.getElementById(`favorito-${autoparteId}`);
    
    if (favoritoItem) {
        favoritoItem.classList.add('removing');
        
        fetch('?handler=Eliminar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: `autoparteId=${autoparteId}&__RequestVerificationToken=${document.querySelector('input[name="__RequestVerificationToken"]').value}`
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                favoritoItem.style.height = `${favoritoItem.offsetHeight}px`;
                
                setTimeout(() => {
                    favoritoItem.style.height = '0';
                    favoritoItem.style.opacity = '0';
                    favoritoItem.style.margin = '0';
                    favoritoItem.style.padding = '0';
                    
                    setTimeout(() => {
                        favoritoItem.remove();
                        
                        // Si no quedan favoritos, recargar para mostrar estado vacío
                        const remainingItems = document.querySelectorAll('.favorito-item');
                        if (remainingItems.length === 0) {
                            buscarFavoritos(document.getElementById('searchInput')?.value.trim() || '');
                        }
                    }, 300);
                }, 10);
                
                mostrarNotificacion('Eliminado de favoritos', 'success');
            } else {
                favoritoItem.classList.remove('removing');
                mostrarNotificacion(data.message || 'Error al eliminar favorito', 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            favoritoItem.classList.remove('removing');
            mostrarNotificacion('Error al eliminar favorito', 'error');
        });
    }
}

function añadirAlCarritoDesdeVista(autoparteId, cantidad) {
    const button = document.querySelector(`#favorito-${autoparteId} .btn-cart`);
    const originalHtml = button.innerHTML;
    
    button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Añadiendo...';
    button.disabled = true;
    
    fetch('/Carrito/AñadirAlCarrito', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: `autoparteId=${autoparteId}&cantidad=${cantidad}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            button.innerHTML = '<i class="fas fa-check me-2"></i>Añadido';
            mostrarNotificacion('Producto añadido al carrito', 'success');
            
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        } else {
            mostrarNotificacion(data.message || 'Error al añadir al carrito', 'error');
            button.innerHTML = originalHtml;
            button.disabled = false;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        mostrarNotificacion('Error al añadir al carrito', 'error');
        button.innerHTML = originalHtml;
        button.disabled = false;
    });
}
function mostrarNotificacion(mensaje, tipo) {
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${tipo === 'success' ? 'success' : 'danger'} border-0 position-fixed`;
    toast.style.cssText = 'bottom: 20px; right: 20px; z-index: 9999; min-width: 250px;';
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="fas fa-${tipo === 'success' ? 'check-circle' : 'times-circle'} me-2"></i>
                ${mensaje}
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