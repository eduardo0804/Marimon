// Función para establecer fechas por defecto
function establecerFechasDefecto() {
    const hoy = new Date();
    const fechaHoy = hoy.toISOString().split('T')[0];
    
    // Calcular fecha de un mes después
    const fechaFinMes = new Date(hoy);
    fechaFinMes.setMonth(fechaFinMes.getMonth() + 1);
    const fechaFinMesStr = fechaFinMes.toISOString().split('T')[0];
    
    // Establecer fechas por defecto para el modal de aplicar oferta
    document.getElementById('ofe_fecha_inicio_aplicar').value = fechaHoy;
    document.getElementById('ofe_fecha_fin_aplicar').value = fechaFinMesStr;
    
    // Establecer fechas por defecto para el modal de editar oferta
    document.getElementById('ofe_fecha_inicio_editar').value = fechaHoy;
    document.getElementById('ofe_fecha_fin_editar').value = fechaFinMesStr;
}

// Función para limpiar errores de validación
function limpiarErroresValidacion(tipo) {
    const errores = [
        `error_descripcion_${tipo}`,
        `error_porcentaje_${tipo}`,
        `error_fecha_inicio_${tipo}`,
        `error_fecha_fin_${tipo}`,
        `error_fechas_${tipo}`
    ];
    
    errores.forEach(errorId => {
        const errorElement = document.getElementById(errorId);
        if (errorElement) {
            errorElement.style.display = 'none';
            errorElement.textContent = '';
        }
    });
    
    // Remover clases de error de los inputs
    const inputs = [
        `ofe_descripcion_${tipo}`,
        `ofe_porcentaje_${tipo}`,
        `ofe_fecha_inicio_${tipo}`,
        `ofe_fecha_fin_${tipo}`
    ];
    
    inputs.forEach(inputId => {
        const inputElement = document.getElementById(inputId);
        if (inputElement) {
            inputElement.classList.remove('is-invalid');
        }
    });
}

// Función para mostrar error de validación
function mostrarErrorValidacion(inputId, errorId, mensaje) {
    const inputElement = document.getElementById(inputId);
    const errorElement = document.getElementById(errorId);
    
    if (inputElement && errorElement) {
        inputElement.classList.add('is-invalid');
        errorElement.textContent = mensaje;
        errorElement.style.display = 'block';
    }
}

// Función para validar formulario de aplicar/editar oferta
function validarFormularioOferta(tipo) {
    limpiarErroresValidacion(tipo);
    let esValido = true;
    
    // Validar descripción
    const descripcion = document.getElementById(`ofe_descripcion_${tipo}`).value.trim();
    if (!descripcion) {
        mostrarErrorValidacion(`ofe_descripcion_${tipo}`, `error_descripcion_${tipo}`, 
            'La descripción de la oferta es obligatoria.');
        esValido = false;
    }
    
    // Validar porcentaje
    const porcentaje = parseFloat(document.getElementById(`ofe_porcentaje_${tipo}`).value);
    if (!porcentaje || porcentaje <= 0 || porcentaje >= 100) {
        mostrarErrorValidacion(`ofe_porcentaje_${tipo}`, `error_porcentaje_${tipo}`, 
            'El porcentaje debe ser entre 1 y 99.');
        esValido = false;
    }
    
    // Validar fecha de inicio
    const fechaInicio = document.getElementById(`ofe_fecha_inicio_${tipo}`).value;
    if (!fechaInicio) {
        mostrarErrorValidacion(`ofe_fecha_inicio_${tipo}`, `error_fecha_inicio_${tipo}`, 
            'La fecha de inicio es obligatoria.');
        esValido = false;
    }
    
    // Validar fecha de fin
    const fechaFin = document.getElementById(`ofe_fecha_fin_${tipo}`).value;
    if (!fechaFin) {
        mostrarErrorValidacion(`ofe_fecha_fin_${tipo}`, `error_fecha_fin_${tipo}`, 
            'La fecha de fin es obligatoria.');
        esValido = false;
    }
    
    // Validar que fecha de inicio sea anterior a fecha de fin
    if (fechaInicio && fechaFin && fechaInicio >= fechaFin) {
        const errorElement = document.getElementById(`error_fechas_${tipo}`);
        if (errorElement) {
            errorElement.textContent = 'La fecha de inicio debe ser anterior a la fecha de fin.';
            errorElement.style.display = 'block';
        }
        esValido = false;
    }
    
    return esValido;
}

// Llamar a la función cuando se carga la página
document.addEventListener('DOMContentLoaded', function() {
    establecerFechasDefecto();
});

// Seleccionar/deseleccionar todos los checkboxes
document.getElementById('selectAll').addEventListener('change', function() {
    const checkboxes = document.querySelectorAll('.producto-check');
    checkboxes.forEach(checkbox => {
        checkbox.checked = this.checked;
    });
});

// Función para verificar si hay productos seleccionados
function verificarProductosSeleccionados() {
    const checkboxesSeleccionados = document.querySelectorAll('.producto-check:checked');
    return checkboxesSeleccionados.length > 0;
}

// Función para limpiar alertas existentes antes de mostrar una nueva
function limpiarAlertasExistentes() {
    const alertasExistentes = document.querySelectorAll('.alert-danger');
    alertasExistentes.forEach(alerta => {
        if (alerta.textContent.includes('Debe seleccionar') || 
            alerta.textContent.includes('no tienen ofertas')) {
            alerta.remove();
        }
    });
}

// Función para mostrar mensaje de error
function mostrarMensajeError(mensaje) {
    // Limpiar alertas existentes primero
    limpiarAlertasExistentes();
    
    // Crear el elemento de alerta
    const alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-danger alert-dismissible fade show';
    alertDiv.setAttribute('role', 'alert');
    alertDiv.innerHTML = `
        ${mensaje}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    // Insertar la alerta después del breadcrumb
    const breadcrumb = document.querySelector('.breadcrumb');
    breadcrumb.parentNode.insertBefore(alertDiv, breadcrumb.nextSibling);
    
    // Auto-remover la alerta después de 5 segundos
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
    
    // Hacer scroll hacia arriba para que se vea la alerta
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// Función para copiar checkboxes seleccionados a los modales
function copiarCheckboxesSeleccionados(containerId) {
    const container = document.getElementById(containerId);
    container.innerHTML = ''; // Limpiar contenedor
    
    const checkboxesSeleccionados = document.querySelectorAll('.producto-check:checked');
    checkboxesSeleccionados.forEach(checkbox => {
        const hiddenInput = document.createElement('input');
        hiddenInput.type = 'hidden';
        hiddenInput.name = 'productosSeleccionados';
        hiddenInput.value = checkbox.value;
        container.appendChild(hiddenInput);
    });
}

// Función para validar productos específicamente para editar
function verificarProductosConOferta() {
    const checkboxesSeleccionados = document.querySelectorAll('.producto-check:checked');
    let productosConOferta = 0;
    
    checkboxesSeleccionados.forEach(checkbox => {
        const tieneOferta = checkbox.getAttribute('data-tiene-oferta');
        if (tieneOferta === 'true') {
            productosConOferta++;
        }
    });
    
    return {
        total: checkboxesSeleccionados.length,
        conOferta: productosConOferta
    };
}

// Event listeners para los botones con validaciones mejoradas
document.getElementById('btnAplicarOferta').addEventListener('click', function() {
    if (!verificarProductosSeleccionados()) {
        mostrarMensajeError('Debe seleccionar al menos un producto para aplicar una oferta.');
        return;
    }
    
    // Establecer fechas por defecto cada vez que se abre el modal
    establecerFechasDefecto();
    
    // Si pasa la validación, copiar checkboxes y abrir modal
    copiarCheckboxesSeleccionados('productosAplicarContainer');
    const modal = new bootstrap.Modal(document.getElementById('aplicarOfertaModal'));
    modal.show();
});

document.getElementById('btnEditarOferta').addEventListener('click', function() {
    if (!verificarProductosSeleccionados()) {
        mostrarMensajeError('Debe seleccionar al menos un producto para editar ofertas.');
        return;
    }
    
    const validacion = verificarProductosConOferta();
    if (validacion.conOferta === 0) {
        mostrarMensajeError('Los productos seleccionados no tienen ofertas activas para editar.');
        return;
    }
    
    // Establecer fechas por defecto cada vez que se abre el modal
    establecerFechasDefecto();
    
    // Si pasa la validación, copiar checkboxes y abrir modal
    copiarCheckboxesSeleccionados('productosEditarContainer');
    const modal = new bootstrap.Modal(document.getElementById('editarOfertaModal'));
    modal.show();
});

document.getElementById('btnEliminarOferta').addEventListener('click', function() {
    if (!verificarProductosSeleccionados()) {
        mostrarMensajeError('Debe seleccionar al menos un producto para eliminar ofertas.');
        return;
    }
    
    const validacion = verificarProductosConOferta();
    if (validacion.conOferta === 0) {
        mostrarMensajeError('Los productos seleccionados no tienen ofertas para eliminar.');
        return;
    }
    
    // Si pasa la validación, copiar checkboxes y abrir modal
    copiarCheckboxesSeleccionados('productosEliminarContainer');
    const modal = new bootstrap.Modal(document.getElementById('eliminarOfertaModal'));
    modal.show();
});

// Event listeners para validación en tiempo real en modal aplicar
document.getElementById('aplicarOfertaForm').addEventListener('submit', function(e) {
    if (!validarFormularioOferta('aplicar')) {
        e.preventDefault();
        return false;
    }
});

// Event listeners para validación en tiempo real en modal editar
document.getElementById('editarOfertaForm').addEventListener('submit', function(e) {
    if (!validarFormularioOferta('editar')) {
        e.preventDefault();
        return false;
    }
});

// Limpiar errores cuando se abren los modales
document.getElementById('aplicarOfertaModal').addEventListener('show.bs.modal', function() {
    limpiarErroresValidacion('aplicar');
});

document.getElementById('editarOfertaModal').addEventListener('show.bs.modal', function() {
    limpiarErroresValidacion('editar');
});

// Validación en tiempo real mientras escriben
['aplicar', 'editar'].forEach(tipo => {
    // Descripción
    document.getElementById(`ofe_descripcion_${tipo}`).addEventListener('input', function() {
        if (this.value.trim()) {
            this.classList.remove('is-invalid');
            document.getElementById(`error_descripcion_${tipo}`).style.display = 'none';
        }
    });
    
    // Porcentaje
    document.getElementById(`ofe_porcentaje_${tipo}`).addEventListener('input', function() {
        const valor = parseFloat(this.value);
        if (valor > 0 && valor < 100) {
            this.classList.remove('is-invalid');
            document.getElementById(`error_porcentaje_${tipo}`).style.display = 'none';
        }
    });
    
    // Fechas
    document.getElementById(`ofe_fecha_inicio_${tipo}`).addEventListener('change', function() {
        if (this.value) {
            this.classList.remove('is-invalid');
            document.getElementById(`error_fecha_inicio_${tipo}`).style.display = 'none';
            document.getElementById(`error_fechas_${tipo}`).style.display = 'none';
        }
    });
    
    document.getElementById(`ofe_fecha_fin_${tipo}`).addEventListener('change', function() {
        if (this.value) {
            this.classList.remove('is-invalid');
            document.getElementById(`error_fecha_fin_${tipo}`).style.display = 'none';
            document.getElementById(`error_fechas_${tipo}`).style.display = 'none';
        }
    });
});