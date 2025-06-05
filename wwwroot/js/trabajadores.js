// Función para crear trabajador
document
  .getElementById("formCrearTrabajador")
  .addEventListener("submit", function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const data = Object.fromEntries(formData);

    // Limpiar errores previos
    limpiarErroresValidacion("formCrearTrabajador");

    // Deshabilitar el botón de envío para evitar múltiples clicks
    const submitBtn = this.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Procesando...';

    fetch("/Trabajadores/CrearTrabajador", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: document.querySelector(
          'input[name="__RequestVerificationToken"]'
        )?.value,
      },
      body: JSON.stringify(data),
    })
      .then((response) => response.json())
      .then((result) => {
        // Restaurar botón
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;

        if (result.success) {
          // Cerrar modal primero
          const modalInstance = bootstrap.Modal.getInstance(
            document.getElementById("modalCrearTrabajador")
          );
          modalInstance.hide();
          
          // Esperar a que el modal se cierre completamente antes de mostrar alerta
          document.getElementById("modalCrearTrabajador").addEventListener('hidden.bs.modal', function onHidden() {
            // Remover este listener para evitar ejecuciones múltiples
            this.removeEventListener('hidden.bs.modal', onHidden);
            
            mostrarAlerta("success", result.message);
            actualizarTablaTrabajadores();
            document.getElementById("formCrearTrabajador").reset();
            
            // Asegurar que no hay overlays residuales
            limpiarModalesResiduo();
          }, { once: true });
          
        } else {
          mostrarErroresValidacionServidor("formCrearTrabajador", result.errors);
        }
      })
      .catch((error) => {
        // Restaurar botón en caso de error
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        
        console.error("Error:", error);
        mostrarAlerta("error", "Error al crear el trabajador");
        limpiarModalesResiduo();
      });
  });

// Función para editar trabajador
function editarUsuario(id, email, rol) {
  // Obtener datos del servidor para asegurar consistencia
  fetch(`/Trabajadores/ObtenerTrabajador?id=${id}`)
    .then((response) => response.json())
    .then((result) => {
      if (result.success) {
        const data = result.data;
        
        // Llenar el formulario con los datos
        document.querySelector('#formEditarTrabajador input[name="Id"]').value = data.Id;
        document.querySelector('#formEditarTrabajador input[name="Email"]').value = data.Email;
        document.querySelector('#formEditarTrabajador select[name="Rol"]').value = data.Rol;
        
        // Limpiar campos de contraseña
        document.querySelector('#formEditarTrabajador input[name="Password"]').value = "";
        document.querySelector('#formEditarTrabajador input[name="ConfirmPassword"]').value = "";

        limpiarErroresValidacion("formEditarTrabajador");

        const modal = new bootstrap.Modal(
          document.getElementById("modalEditarTrabajador")
        );
        modal.show();
      } else {
        mostrarAlerta("error", "Error al obtener los datos del trabajador");
      }
    })
    .catch((error) => {
      console.error("Error:", error);
      mostrarAlerta("error", "Error al obtener los datos del trabajador");
    });
}

document
  .getElementById("formEditarTrabajador")
  .addEventListener("submit", function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const data = Object.fromEntries(formData);

    // Si no hay contraseña, eliminar campos de contraseña del objeto
    if (!data.Password || data.Password.trim() === "") {
      delete data.Password;
      delete data.ConfirmPassword;
    }

    limpiarErroresValidacion("formEditarTrabajador");

    // Deshabilitar el botón de envío
    const submitBtn = this.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Procesando...';

    fetch("/Trabajadores/EditarTrabajador", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: document.querySelector(
          'input[name="__RequestVerificationToken"]'
        )?.value,
      },
      body: JSON.stringify(data),
    })
      .then((response) => response.json())
      .then((result) => {
        // Restaurar botón
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;

        if (result.success) {
          // Cerrar modal primero
          const modalInstance = bootstrap.Modal.getInstance(
            document.getElementById("modalEditarTrabajador")
          );
          modalInstance.hide();
          
          // Esperar a que el modal se cierre completamente
          document.getElementById("modalEditarTrabajador").addEventListener('hidden.bs.modal', function onHidden() {
            this.removeEventListener('hidden.bs.modal', onHidden);
            
            mostrarAlerta("success", result.message);
            actualizarTablaTrabajadores();
            limpiarModalesResiduo();
          }, { once: true });
          
        } else {
          mostrarErroresValidacionServidor("formEditarTrabajador", result.errors);
        }
      })
      .catch((error) => {
        // Restaurar botón en caso de error
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        
        console.error("Error:", error);
        mostrarAlerta("error", "Error al editar el trabajador");
        limpiarModalesResiduo();
      });
  });

// Función para eliminar trabajador
function eliminarUsuario(id, email) {
  document.querySelector('#formEliminarTrabajador input[name="Id"]').value = id;
  document.getElementById("eliminarEmail").textContent = email;

  new bootstrap.Modal(
    document.getElementById("modalEliminarTrabajador")
  ).show();
}

document
  .getElementById("formEliminarTrabajador")
  .addEventListener("submit", function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const data = Object.fromEntries(formData);

    // Deshabilitar el botón de envío
    const submitBtn = this.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Eliminando...';

    fetch("/Trabajadores/EliminarTrabajador", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: document.querySelector(
          'input[name="__RequestVerificationToken"]'
        )?.value,
      },
      body: JSON.stringify(data),
    })
      .then((response) => response.json())
      .then((result) => {
        // Restaurar botón
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;

        if (result.success) {
          // Cerrar modal primero
          const modalInstance = bootstrap.Modal.getInstance(
            document.getElementById("modalEliminarTrabajador")
          );
          modalInstance.hide();
          
          // Esperar a que el modal se cierre completamente
          document.getElementById("modalEliminarTrabajador").addEventListener('hidden.bs.modal', function onHidden() {
            this.removeEventListener('hidden.bs.modal', onHidden);
            
            mostrarAlerta("success", result.message);
            actualizarTablaTrabajadores();
            limpiarModalesResiduo();
          }, { once: true });
          
        } else {
          if (result.errors) {
            mostrarAlerta("error", result.errors.join("<br>"));
          } else {
            mostrarAlerta("error", result.error);
          }
        }
      })
      .catch((error) => {
        // Restaurar botón en caso de error
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        
        console.error("Error:", error);
        mostrarAlerta("error", "Error al eliminar el trabajador");
        limpiarModalesResiduo();
      });
  });

// Función para actualizar tabla sin recargar la página completa
function actualizarTablaTrabajadores() {
  fetch("/Trabajadores/Index")
    .then((response) => response.text())
    .then((html) => {
      // Crear un documento temporal para extraer solo la tabla
      const tempDiv = document.createElement("div");
      tempDiv.innerHTML = html;

      // Extraer la nueva tabla
      const nuevaTabla = tempDiv.querySelector(".table-responsive");
      const tablaActual = document.querySelector(".table-responsive");

      if (nuevaTabla && tablaActual) {
        tablaActual.innerHTML = nuevaTabla.innerHTML;
      } else {
        // Si no se encuentra la tabla, intentar actualizar el contenido completo de la card
        const nuevaCard = tempDiv.querySelector(".card-body");
        const cardActual = document.querySelector(".card-body");
        
        if (nuevaCard && cardActual) {
          cardActual.innerHTML = nuevaCard.innerHTML;
        }
      }
    })
    .catch((error) => {
      console.error("Error al actualizar tabla:", error);
      // Si falla la actualización, hacer recarga completa como fallback
      location.reload();
    });
}

// Función para limpiar errores de validación
function limpiarErroresValidacion(formId) {
  const form = document.getElementById(formId);
  const validationSpans = form.querySelectorAll(".text-danger");
  validationSpans.forEach((span) => {
    span.textContent = "";
  });
  
  // Remover clases de error de Bootstrap si las hay
  const inputs = form.querySelectorAll(".form-control, .form-select");
  inputs.forEach((input) => {
    input.classList.remove("is-invalid");
  });
}

// Función para mostrar errores de validación del servidor
function mostrarErroresValidacionServidor(formId, errores) {
  const form = document.getElementById(formId);
  
  errores.forEach((error) => {
    let targetSpan = null;
    
    // Mapear errores a campos específicos
    if (error.includes("correo") || error.includes("Email") || error.includes("electrónico")) {
      targetSpan = form.querySelector('span[data-valmsg-for*="Email"]') || 
                   form.querySelector('input[name="Email"]').nextElementSibling;
    } else if (error.includes("contraseña") || error.includes("Password")) {
      if (error.includes("coinciden") || error.includes("Confirm")) {
        targetSpan = form.querySelector('span[data-valmsg-for*="ConfirmPassword"]') || 
                     form.querySelector('input[name="ConfirmPassword"]').nextElementSibling;
      } else {
        targetSpan = form.querySelector('span[data-valmsg-for*="Password"]') || 
                     form.querySelector('input[name="Password"]').nextElementSibling;
      }
    } else if (error.includes("rol") || error.includes("Rol")) {
      targetSpan = form.querySelector('span[data-valmsg-for*="Rol"]') || 
                   form.querySelector('select[name="Rol"]').nextElementSibling;
    }
    
    // Si encontramos el span de validación, mostrar el error
    if (targetSpan && targetSpan.classList.contains("text-danger")) {
      targetSpan.textContent = error;
      
      // Agregar clase de error al input correspondiente
      const input = targetSpan.previousElementSibling;
      if (input) {
        input.classList.add("is-invalid");
      }
    }
  });
}

// Función para mostrar alertas
function mostrarAlerta(tipo, mensaje) {
  // Remover alertas anteriores
  const alertasAnteriores = document.querySelectorAll('.alert');
  alertasAnteriores.forEach(alerta => alerta.remove());

  const alertHtml = `
        <div class="alert alert-${
          tipo === "success" ? "success" : "danger"
        } alert-dismissible fade show" role="alert">
            <i class="fas fa-${
              tipo === "success" ? "check-circle" : "exclamation-circle"
            }"></i>
            ${mensaje}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

  const container = document.querySelector(".container-fluid");
  container.insertAdjacentHTML("afterbegin", alertHtml);
  
  // Auto-hide success alerts after 5 seconds
  if (tipo === "success") {
    setTimeout(() => {
      const alert = container.querySelector(".alert-success");
      if (alert) {
        const bsAlert = new bootstrap.Alert(alert);
        bsAlert.close();
      }
    }, 5000);
  }
}

// Nueva función para limpiar residuos de modales
function limpiarModalesResiduo() {
  // Remover cualquier backdrop residual
  const backdrops = document.querySelectorAll('.modal-backdrop');
  backdrops.forEach(backdrop => backdrop.remove());
  
  // Remover clases del body que pueden quedar
  document.body.classList.remove('modal-open');
  document.body.style.overflow = '';
  document.body.style.paddingRight = '';
  
  // Restaurar el scroll si está bloqueado
  document.documentElement.style.overflow = '';
}

// Limpiar formularios cuando se cierran los modales
document
  .getElementById("modalCrearTrabajador")
  .addEventListener("hidden.bs.modal", function () {
    document.getElementById("formCrearTrabajador").reset();
    limpiarErroresValidacion("formCrearTrabajador");
    limpiarModalesResiduo();
  });

document
  .getElementById("modalEditarTrabajador")
  .addEventListener("hidden.bs.modal", function () {
    limpiarErroresValidacion("formEditarTrabajador");
    limpiarModalesResiduo();
  });

document
  .getElementById("modalEliminarTrabajador")
  .addEventListener("hidden.bs.modal", function () {
    limpiarErroresValidacion("formEliminarTrabajador");
    limpiarModalesResiduo();
  });

// Función adicional para manejar clicks en botones después de acciones
document.addEventListener('DOMContentLoaded', function() {
  // Asegurar que los eventos de click funcionen correctamente después de actualizaciones
  document.addEventListener('click', function(e) {
    // Si hay algún overlay residual, removerlo
    if (e.target.closest('.modal-backdrop')) {
      limpiarModalesResiduo();
    }
  });
  
  // Manejar el escape key para cerrar modales problemáticos
  document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
      limpiarModalesResiduo();
    }
  });
});