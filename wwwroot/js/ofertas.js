document.addEventListener('DOMContentLoaded', function() {
    const selectAllCheckbox = document.getElementById('selectAll');
    const productoCheckboxes = document.querySelectorAll('.producto-check');
    const tablaContainer = document.getElementById('tablaContainer');
    const btnAplicarOferta = document.getElementById('btnAplicarOferta');
    const btnEditarOferta = document.getElementById('btnEditarOferta');
    const modalOfertaBatch = new bootstrap.Modal(document.getElementById('modalOfertaBatch'));
    const formProductos = document.getElementById('productosForm');
    const actionInput = document.getElementById('actionInput');
    
    // Función para marcar/desmarcar todos los checkboxes
    selectAllCheckbox.addEventListener('change', function() {
        const isChecked = this.checked;
        productoCheckboxes.forEach(checkbox => {
            checkbox.checked = isChecked;
        });
    });
    
    // Verificar si todos los checkboxes están marcados
    function updateSelectAllCheckbox() {
        const totalCheckboxes = productoCheckboxes.length;
        const checkedCount = Array.from(productoCheckboxes).filter(checkbox => checkbox.checked).length;
        
        selectAllCheckbox.checked = totalCheckboxes > 0 && checkedCount === totalCheckboxes;
        selectAllCheckbox.indeterminate = checkedCount > 0 && checkedCount < totalCheckboxes;
    }
    
    // Agregar evento a cada checkbox individual
    productoCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', updateSelectAllCheckbox);
    });

    // Manejar el botón "Aplicar Oferta"
    btnAplicarOferta.addEventListener('click', function() {
        const productosSeleccionados = Array.from(productoCheckboxes)
            .filter(checkbox => checkbox.checked)
            .map(checkbox => checkbox.value);
        
        if (productosSeleccionados.length === 0) {
            alert('Debe seleccionar al menos un producto para aplicar la oferta.');
            return;
        }
        
        // Limpiar el contenedor de productos seleccionados
        const container = document.getElementById('productosSeleccionadosContainer');
        container.innerHTML = '';
        
        // Agregar campos ocultos para cada producto seleccionado
        productosSeleccionados.forEach(function(productoId) {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'productosSeleccionados';
            input.value = productoId;
            container.appendChild(input);
        });
        
        // Configurar fechas por defecto
        const today = new Date();
        const endDate = new Date();
        endDate.setDate(today.getDate() + 30); // Por defecto, 30 días de oferta
        
        document.getElementById('fechaInicioBatch').valueAsDate = today;
        document.getElementById('fechaFinBatch').valueAsDate = endDate;
        
        // Asegurar que el modal esté configurado para aplicar
        document.getElementById('modalOfertaBatchLabel').textContent = 'Aplicar Oferta a Productos Seleccionados';
        document.querySelector('#formOfertaBatch input[name="action"]').value = 'aplicar';
        
        // Mostrar el modal
        modalOfertaBatch.show();
    });
    
    // MODIFICADO: Manejar el botón "Editar Oferta"
    btnEditarOferta.addEventListener('click', function() {
        const productosSeleccionados = Array.from(productoCheckboxes)
            .filter(checkbox => checkbox.checked);
        
        if (productosSeleccionados.length === 0) {
            alert('Debe seleccionar al menos un producto para editar su oferta.');
            return;
        }
        
        // Verificar que todos los productos seleccionados tengan oferta
        const productosConOferta = productosSeleccionados.filter(checkbox => 
            checkbox.dataset.tieneOferta === "true"
        );
        
        if (productosConOferta.length === 0) {
            alert('Ninguno de los productos seleccionados tiene oferta para editar.');
            return;
        }
        
        if (productosConOferta.length !== productosSeleccionados.length) {
            const sinOferta = productosSeleccionados.length - productosConOferta.length;
            if (!confirm(`${sinOferta} producto(s) seleccionado(s) no tienen oferta. ¿Desea continuar editando solo los que sí tienen oferta?`)) {
                return;
            }
        }
        
        // Limpiar el contenedor de productos seleccionados
        const container = document.getElementById('productosSeleccionadosContainer');
        container.innerHTML = '';
        
        // Agregar campos ocultos solo para productos con oferta
        productosConOferta.forEach(function(checkbox) {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'productosSeleccionados';
            input.value = checkbox.value;
            container.appendChild(input);
        });
        
        // Configurar fechas por defecto
        const today = new Date();
        const endDate = new Date();
        endDate.setDate(today.getDate() + 30);
        
        document.getElementById('fechaInicioBatch').valueAsDate = today;
        document.getElementById('fechaFinBatch').valueAsDate = endDate;
        
        // Configurar el modal para edición
        document.getElementById('modalOfertaBatchLabel').textContent = 'Editar Oferta de Productos Seleccionados';
        document.querySelector('#formOfertaBatch input[name="action"]').value = 'editar';
        
        // Mostrar el modal
        modalOfertaBatch.show();
    });

    // Validar antes de enviar el formulario cuando es para eliminar
    if (formProductos) {
        formProductos.addEventListener('submit', function(e) {
            // Solo validamos si es eliminar, el resto se maneja por botones personalizados
            if (e.submitter && e.submitter.value === 'eliminar') {
                const productosSeleccionados = Array.from(document.querySelectorAll('.producto-check:checked')).map(c => c.value);
                
                if (productosSeleccionados.length === 0) {
                    e.preventDefault();
                    alert('Debe seleccionar al menos un producto para eliminar su oferta.');
                    return false;
                }
                
                const mensaje = productosSeleccionados.length === 1 
                    ? '¿Está seguro de que desea eliminar la oferta del producto seleccionado?' 
                    : `¿Está seguro de que desea eliminar las ofertas de los ${productosSeleccionados.length} productos seleccionados?`;
                
                if (!confirm(mensaje)) {
                    e.preventDefault();
                    return false;
                }
            }
        });
    }

    // NUEVO: Resetear el modal cuando se cierre
    document.getElementById('modalOfertaBatch').addEventListener('hidden.bs.modal', function () {
        // Resetear el título
        document.getElementById('modalOfertaBatchLabel').textContent = 'Aplicar Oferta a Productos Seleccionados';
        
        // Resetear el action
        document.querySelector('#formOfertaBatch input[name="action"]').value = 'aplicar';
        
        // Limpiar el formulario
        document.getElementById('formOfertaBatch').reset();
    });

    // Función para scroll con tecla presionada
    let isScrollMode = false;
    let isDragging = false;
    let startY = 0;
    let scrollStartY = 0;

    // Cambiar cursor y activar modo scroll al presionar la tecla (por ejemplo, Ctrl)
    document.addEventListener('keydown', function(e) {
        if (e.ctrlKey && !isScrollMode) {
            isScrollMode = true;
            tablaContainer.style.cursor = 'grab';
        }
    });

    // Desactivar modo scroll al soltar la tecla
    document.addEventListener('keyup', function(e) {
        if (!e.ctrlKey && isScrollMode) {
            isScrollMode = false;
            isDragging = false;
            tablaContainer.style.cursor = 'default';
        }
    });

    // Eventos de mouse para el scroll manual
    tablaContainer.addEventListener('mousedown', function(e) {
        if (isScrollMode) {
            isDragging = true;
            startY = e.clientY;
            scrollStartY = tablaContainer.scrollTop;
            tablaContainer.style.cursor = 'grabbing';
            e.preventDefault();
        }
    });

    document.addEventListener('mousemove', function(e) {
        if (isDragging && isScrollMode) {
            const deltaY = startY - e.clientY;
            tablaContainer.scrollTop = scrollStartY + deltaY;
            e.preventDefault();
        }
    });

    document.addEventListener('mouseup', function() {
        if (isDragging && isScrollMode) {
            isDragging = false;
            tablaContainer.style.cursor = 'grab';
        }
    });

    // Evitar la selección de texto durante el arrastre
    tablaContainer.addEventListener('selectstart', function(e) {
        if (isScrollMode) {
            e.preventDefault();
        }
    });
});