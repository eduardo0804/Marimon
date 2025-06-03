// Función para crear trabajador
document
  .getElementById("formCrearTrabajador")
  .addEventListener("submit", function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const data = Object.fromEntries(formData);

    // Validar que las contraseñas coincidan
    if (data.Password !== data.ConfirmPassword) {
      mostrarError("crearConfirmPassword", "Las contraseñas no coinciden");
      return;
    }

    // Limpiar errores previos
    limpiarErrores("formCrearTrabajador");

    fetch('@Url.Action("CrearTrabajador", "Trabajadores")', {
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
        if (result.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalCrearTrabajador")
          ).hide();
          mostrarAlerta("success", result.message);
          // REMOVIDO: setTimeout(() => location.reload(), 1500);
          // Opcional: actualizar la tabla sin recargar toda la página
          actualizarTablaTrabajadores();
        } else {
          mostrarErroresValidacion("formCrearTrabajador", result.errors);
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        mostrarAlerta("error", "Error al crear el trabajador");
      });
  });

// Función para crear trabajador
document
  .getElementById("formCrearTrabajador")
  .addEventListener("submit", function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const data = Object.fromEntries(formData);

    // Validar que las contraseñas coincidan
    if (data.Password !== data.ConfirmPassword) {
      mostrarError("crearConfirmPassword", "Las contraseñas no coinciden");
      return;
    }

    // Limpiar errores previos
    limpiarErrores("formCrearTrabajador");

    fetch('@Url.Action("CrearTrabajador", "Trabajadores")', {
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
        if (result.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalCrearTrabajador")
          ).hide();
          mostrarAlerta("success", result.message);
          // REMOVIDO: setTimeout(() => location.reload(), 1500);
          // Opcional: actualizar la tabla sin recargar toda la página
          actualizarTablaTrabajadores();
        } else {
          mostrarErroresValidacion("formCrearTrabajador", result.errors);
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        mostrarAlerta("error", "Error al crear el trabajador");
      });
  });

// Función para editar trabajador
function editarUsuario(id, email, rol) {
  document.getElementById("editarId").value = id;
  document.getElementById("editarEmail").value = email;
  document.getElementById("editarRol").value = rol;

  document.getElementById("editarPassword").value = "";
  document.getElementById("editarConfirmPassword").value = "";

  limpiarErrores("formEditarTrabajador");

  const modal = new bootstrap.Modal(
    document.getElementById("modalEditarTrabajador")
  );
  modal.show();

  console.log("Modal mostrado con email:", email);
}

document
  .getElementById("formEditarTrabajador")
  .addEventListener("submit", function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const data = Object.fromEntries(formData);

    // Validar que las contraseñas coincidan SOLO si se proporcionó una nueva contraseña
    if (data.Password && data.Password.trim() !== "") {
      if (data.Password !== data.ConfirmPassword) {
        mostrarError("editarConfirmPassword", "Las contraseñas no coinciden");
        return;
      }
    } else {
      delete data.Password;
      delete data.ConfirmPassword;
    }

    limpiarErrores("formEditarTrabajador");

    fetch('@Url.Action("EditarTrabajador", "Trabajadores")', {
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
        if (result.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalEditarTrabajador")
          ).hide();
          mostrarAlerta("success", result.message);
          // REMOVIDO: setTimeout(() => location.reload(), 1500);
          // Opcional: actualizar la tabla sin recargar toda la página
          actualizarTablaTrabajadores();
        } else {
          mostrarErroresValidacion("formEditarTrabajador", result.errors);
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        mostrarAlerta("error", "Error al editar el trabajador");
      });
  });

// Función para eliminar trabajador
function eliminarUsuario(id, email) {
  document.getElementById("eliminarId").value = id;
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

    fetch('@Url.Action("EliminarTrabajador", "Trabajadores")', {
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
        if (result.success) {
          bootstrap.Modal.getInstance(
            document.getElementById("modalEliminarTrabajador")
          ).hide();
          mostrarAlerta("success", result.message);
          // REMOVIDO: setTimeout(() => location.reload(), 1500);
          // Opcional: actualizar la tabla sin recargar toda la página
          actualizarTablaTrabajadores();
        } else {
          if (result.errors) {
            mostrarAlerta("error", result.errors.join("<br>"));
          } else {
            mostrarAlerta("error", result.error);
          }
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        mostrarAlerta("error", "Error al eliminar el trabajador");
      });
  });

// NUEVA FUNCIÓN: Actualizar tabla sin recargar la página completa
function actualizarTablaTrabajadores() {
  fetch('@Url.Action("Index", "Trabajadores")')
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
      }
    })
    .catch((error) => {
      console.error("Error al actualizar tabla:", error);
      // Si falla la actualización, hacer recarga completa como fallback
      location.reload();
    });
}

// Funciones auxiliares (sin cambios)
function mostrarError(inputId, mensaje) {
  const input = document.getElementById(inputId);
  input.classList.add("is-invalid");
  const feedback = input.nextElementSibling;
  if (feedback && feedback.classList.contains("invalid-feedback")) {
    feedback.textContent = mensaje;
  }
}

function limpiarErrores(formId) {
  const form = document.getElementById(formId);
  const inputs = form.querySelectorAll(".form-control, .form-select");
  inputs.forEach((input) => {
    input.classList.remove("is-invalid");
    const feedback = input.nextElementSibling;
    if (feedback && feedback.classList.contains("invalid-feedback")) {
      feedback.textContent = "";
    }
  });
}

function mostrarErroresValidacion(formId, errores) {
  const form = document.getElementById(formId);
  errores.forEach((error) => {
    if (error.includes("correo") || error.includes("Email")) {
      const emailInput = form.querySelector('input[name="Email"]');
      if (emailInput) mostrarError(emailInput.id, error);
    } else if (error.includes("contraseña") || error.includes("Password")) {
      const passwordInput = form.querySelector('input[name="Password"]');
      if (passwordInput) mostrarError(passwordInput.id, error);
    } else if (error.includes("rol") || error.includes("Rol")) {
      const rolSelect = form.querySelector('select[name="Rol"]');
      if (rolSelect) mostrarError(rolSelect.id, error);
    }
  });
}

function mostrarAlerta(tipo, mensaje) {
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
}

// Limpiar formularios cuando se cierran los modales
document
  .getElementById("modalCrearTrabajador")
  .addEventListener("hidden.bs.modal", function () {
    document.getElementById("formCrearTrabajador").reset();
    limpiarErrores("formCrearTrabajador");
  });

document
  .getElementById("modalEditarTrabajador")
  .addEventListener("hidden.bs.modal", function () {
    limpiarErrores("formEditarTrabajador");
  });
