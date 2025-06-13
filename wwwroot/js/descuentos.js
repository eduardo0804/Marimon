// Función para establecer fechas por defecto
function establecerFechasDefecto() {
    const hoy = new Date();
    const fechaHoy = hoy.toISOString().split('T')[0];
    
    // Calcular fecha de un mes después
    const fechaFinMes = new Date(hoy);
    fechaFinMes.setMonth(fechaFinMes.getMonth() + 1);
    const fechaFinMesStr = fechaFinMes.toISOString().split('T')[0];
    
    // Establecer fechas por defecto para el modal de aplicar descuento
    document.getElementById('cod_fecha_creacion').value = fechaHoy;
    document.getElementById('cod_fecha_expiracion').value = fechaFinMesStr;
}

// Función para limpiar errores de validación
function limpiarErroresValidacion() {
    const errores = [
        'error_codigo',
        'error_descripcion',
        'error_porcentaje',
        'error_fecha_inicio',
        'error_fecha_fin',
        'error_fechas'
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
        'cod_codigo',
        'cod_descripcion',
        'cod_porcentaje',
        'cod_fecha_creacion',
        'cod_fecha_expiracion'
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

// Función para validar formulario de aplicar descuento
function validarFormularioDescuento() {
    limpiarErroresValidacion();
    let esValido = true;
    
    // Validar código de descuento
    const codigo = document.getElementById('cod_codigo').value.trim();
    if (!codigo) {
        mostrarErrorValidacion('cod_codigo', 'error_codigo', 
            'El código de descuento es obligatorio.');
        esValido = false;
    }
    
    // Validar descripción
    const descripcion = document.getElementById('cod_descripcion').value.trim();
    if (!descripcion) {
        mostrarErrorValidacion('cod_descripcion', 'error_descripcion', 
            'La descripción del código de descuento es obligatoria.');
        esValido = false;
    }
    
    // Validar porcentaje
    const porcentaje = parseFloat(document.getElementById('cod_porcentaje').value);
    if (!porcentaje || porcentaje <= 0 || porcentaje >= 100) {
        mostrarErrorValidacion('cod_porcentaje', 'error_porcentaje', 
            'El porcentaje debe ser entre 1 y 99.');
        esValido = false;
    }
    
    // Validar fecha de inicio
    const fechaInicio = document.getElementById('cod_fecha_creacion').value;
    if (!fechaInicio) {
        mostrarErrorValidacion('cod_fecha_creacion', 'error_fecha_inicio', 
            'La fecha de inicio es obligatoria.');
        esValido = false;
    }
    
    // Validar fecha de fin
    const fechaFin = document.getElementById('cod_fecha_expiracion').value;
    if (!fechaFin) {
        mostrarErrorValidacion('cod_fecha_expiracion', 'error_fecha_fin', 
            'La fecha de expiración es obligatoria.');
        esValido = false;
    }
    
    // Validar que fecha de inicio sea anterior a fecha de fin
    if (fechaInicio && fechaFin && fechaInicio >= fechaFin) {
        const errorElement = document.getElementById('error_fechas');
        if (errorElement) {
            errorElement.textContent = 'La fecha de inicio debe ser anterior a la fecha de expiración.';
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
            alerta.textContent.includes('no tienen códigos')) {
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

// Función para validar productos específicamente para eliminar
function verificarProductosConDescuento() {
    const checkboxesSeleccionados = document.querySelectorAll('.producto-check:checked');
    let productosConDescuento = 0;
    
    checkboxesSeleccionados.forEach(checkbox => {
        const tieneDescuento = checkbox.getAttribute('data-tiene-descuento');
        if (tieneDescuento === 'true') {
            productosConDescuento++;
        }
    });
    
    return {
        total: checkboxesSeleccionados.length,
        conDescuento: productosConDescuento
    };
}

// Event listeners para los botones con validaciones mejoradas
document.getElementById('btnAplicarDescuento').addEventListener('click', function() {
    if (!verificarProductosSeleccionados()) {
        mostrarMensajeError('Debe seleccionar al menos un producto para aplicar un código de descuento.');
        return;
    }
    
    // Establecer fechas por defecto cada vez que se abre el modal
    establecerFechasDefecto();
    
    // Si pasa la validación, copiar checkboxes y abrir modal
    copiarCheckboxesSeleccionados('productosAplicarContainer');
    const modal = new bootstrap.Modal(document.getElementById('modalAplicarDescuento'));
    modal.show();
});

document.getElementById('btnEliminarDescuento').addEventListener('click', function() {
    if (!verificarProductosSeleccionados()) {
        mostrarMensajeError('Debe seleccionar al menos un producto para eliminar códigos de descuento.');
        return;
    }
    
    const validacion = verificarProductosConDescuento();
    if (validacion.conDescuento === 0) {
        mostrarMensajeError('Los productos seleccionados no tienen códigos de descuento para eliminar.');
        return;
    }
    
    // Si pasa la validación, copiar checkboxes y abrir modal
    copiarCheckboxesSeleccionados('productosEliminarContainer');
    const modal = new bootstrap.Modal(document.getElementById('modalEliminarDescuento'));
    modal.show();
});

// Event listener modificado para el botón de enviar código - ahora abre modal
document.getElementById('btnEnviarCodigo').addEventListener('click', function() {
    if (!verificarProductosSeleccionados()) {
        mostrarMensajeError('Debe seleccionar al menos un producto para enviar códigos de descuento.');
        return;
    }
    
    const validacion = verificarProductosConDescuento();
    if (validacion.conDescuento === 0) {
        mostrarMensajeError('Los productos seleccionados no tienen códigos de descuento para enviar.');
        return;
    }
    
    // Si pasa la validación, copiar checkboxes y abrir modal
    copiarCheckboxesSeleccionados('productosEnviarContainer');
    const modal = new bootstrap.Modal(document.getElementById('modalEnviarCodigo'));
    modal.show();
});

// Event listener para validación en el modal aplicar
document.getElementById('formAplicarDescuento').addEventListener('submit', function(e) {
    if (!validarFormularioDescuento()) {
        e.preventDefault();
        return false;
    }
});

// Limpiar errores cuando se abre el modal
document.getElementById('modalAplicarDescuento').addEventListener('show.bs.modal', function() {
    limpiarErroresValidacion();
});

// Validación en tiempo real mientras escriben
// Código de descuento
document.getElementById('cod_codigo').addEventListener('input', function() {
    if (this.value.trim()) {
        this.classList.remove('is-invalid');
        document.getElementById('error_codigo').style.display = 'none';
    }
});

// Descripción
document.getElementById('cod_descripcion').addEventListener('input', function() {
    if (this.value.trim()) {
        this.classList.remove('is-invalid');
        document.getElementById('error_descripcion').style.display = 'none';
    }
});

// Porcentaje
document.getElementById('cod_porcentaje').addEventListener('input', function() {
    const valor = parseFloat(this.value);
    if (valor > 0 && valor < 100) {
        this.classList.remove('is-invalid');
        document.getElementById('error_porcentaje').style.display = 'none';
    }
});

// Fecha de inicio
document.getElementById('cod_fecha_creacion').addEventListener('change', function() {
    if (this.value) {
        this.classList.remove('is-invalid');
        document.getElementById('error_fecha_inicio').style.display = 'none';
        document.getElementById('error_fechas').style.display = 'none';
    }
});

// Fecha de expiración
document.getElementById('cod_fecha_expiracion').addEventListener('change', function() {
    if (this.value) {
        this.classList.remove('is-invalid');
        document.getElementById('error_fecha_fin').style.display = 'none';
        document.getElementById('error_fechas').style.display = 'none';
    }
});

// Función para limpiar el formulario cuando se cierra el modal
document.getElementById('modalAplicarDescuento').addEventListener('hidden.bs.modal', function() {
    // Limpiar campos del formulario
    document.getElementById('cod_codigo').value = '';
    document.getElementById('cod_descripcion').value = '';
    document.getElementById('cod_porcentaje').value = '';
    
    // Restablecer fechas por defecto
    establecerFechasDefecto();
    
    // Limpiar errores
    limpiarErroresValidacion();
});