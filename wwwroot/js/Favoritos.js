document.addEventListener("DOMContentLoaded", function() {
    const searchInput = document.getElementById('searchInput');
    const searchClearBtn = document.getElementById('searchClearBtn');
    const searchSubmitBtn = document.getElementById('searchSubmitBtn');
    
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            if (searchClearBtn) {
                searchClearBtn.style.display = this.value.trim() ? 'block' : 'none';
            }
            
            clearTimeout(searchInput.searchTimeout);
            searchInput.searchTimeout = setTimeout(() => {
                buscarFavoritos(this.value.trim());
            }, 300); 
        });
        
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                buscarFavoritos(this.value.trim());
            }
        });
    }
    
    if (searchClearBtn) {
        searchClearBtn.addEventListener('click', function() {
            if (searchInput) {
                searchInput.value = '';
                this.style.display = 'none';
                buscarFavoritos('');
                searchInput.focus();
            }
        });
    }
    
    if (searchSubmitBtn) {
        searchSubmitBtn.addEventListener('click', function() {
            buscarFavoritos(searchInput ? searchInput.value.trim() : '');
        });
    }
    actualizarContadorFavoritos();
});

function buscarFavoritos(termino) {
    const favoritosContainer = document.getElementById('favoritosContainer');
    
    if (!favoritosContainer) return;
    
    favoritosContainer.classList.add('searching');
    favoritosContainer.innerHTML = `
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Buscando...</span>
            </div>
            <p class="mt-3">Buscando en favoritos...</p>
        </div>
    `;
    
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const headers = {
        'X-Requested-With': 'XMLHttpRequest'
    };
    
    if (token) {
        headers['RequestVerificationToken'] = token;
    }
    
    const timestamp = new Date().getTime();
    
    fetch(`?handler=Buscar&termino=${encodeURIComponent(termino)}&_=${timestamp}`, {
        headers: headers
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}`);
        }
        return response.text();
    })
    .then(html => {
        favoritosContainer.classList.remove('searching');
        
        favoritosContainer.innerHTML = html;
        actualizarContadorFavoritos();
        if (termino && termino.trim() !== '') {
            setTimeout(() => {
                resaltarTerminosBusqueda(termino);
            }, 50);
        }
        
        if (!document.querySelector('#favoritoScript')) {
            const script = document.createElement('script');
            script.id = 'favoritoScript';
            script.src = '/js/favoritos.js';
            script.defer = true;
            document.body.appendChild(script);
        }
    })
    .catch(error => {
        console.error('Error en búsqueda:', error);
        favoritosContainer.classList.remove('searching');
        favoritosContainer.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-circle me-2"></i>
                Error al buscar favoritos: ${error.message}
            </div>
            <button class="btn btn-outline-primary mt-3" onclick="window.location.reload()">
                <i class="fas fa-sync me-2"></i>Reintentar
            </button>
        `;
    });
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
                favoritoItem.style.opacity = '0';
                favoritoItem.style.transform = 'scale(0.9)';
                
                setTimeout(() => {
                    favoritoItem.remove();
                    
                    actualizarContadorFavoritos();
                    mostrarNotificacion('Eliminado de favoritos', 'success');
                }, 300);
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
            }, 2000);
        } else {
            mostrarNotificacion(data.message || 'Error al añadir al carrito', 'error');
            button.innerHTML = originalHtml;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        mostrarNotificacion('Error al añadir al carrito', 'error');
        button.innerHTML = originalHtml;
    })
    .finally(() => {
        button.disabled = false;
    });
}

function actualizarContadorFavoritos() {
    const items = document.querySelectorAll('.favorito-item');
    const contadorElement = document.querySelector('small.text-muted');
    
    if (contadorElement) {
        contadorElement.textContent = `${items.length} autopartes guardadas`;
        if (window.FavoritosDM) {
            window.FavoritosDM.TotalFavoritos = items.length;
        }
    }
}

function resaltarTerminosBusqueda(termino) {
    if (!termino) return;
    
    const palabras = termino.split(' ')
        .filter(p => p.trim() !== '')
        .map(p => p.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'));
    
    if (palabras.length === 0) return;
    
    const regex = new RegExp(`(${palabras.join('|')})`, 'gi');
    
    function resaltar(text) {
        return text.replace(regex, '<mark>$1</mark>');
    }
    
    document.querySelectorAll('.card-title, .card-description, .card-text').forEach(elemento => {
        const textoOriginal = elemento.textContent;
        if (regex.test(textoOriginal)) {
            elemento.innerHTML = resaltar(textoOriginal);
        }
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