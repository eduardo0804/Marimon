document.addEventListener("DOMContentLoaded", function () {
  const selectAllCheckbox = document.getElementById("selectAll");
  const productoCheckboxes = document.querySelectorAll(".producto-check");
  const tablaContainer = document.getElementById("tablaContainer");
  const btnAplicarDescuento = document.getElementById("btnAplicarDescuento");
  const btnEliminarDescuento = document.getElementById("btnEliminarDescuento");
  const btnEnviarCodigo = document.getElementById("btnEnviarCodigo");
  const modalAplicarDescuento = new bootstrap.Modal(
    document.getElementById("modalAplicarDescuento")
  );
  const modalEliminarDescuento = new bootstrap.Modal(
    document.getElementById("modalEliminarDescuento")
  );
  const formProductos = document.getElementById("productosForm");
  const actionInput = document.getElementById("actionInput");

  // Función para marcar/desmarcar todos los checkboxes
  selectAllCheckbox.addEventListener("change", function () {
    const isChecked = this.checked;
    productoCheckboxes.forEach((checkbox) => {
      checkbox.checked = isChecked;
    });
  });

  // Verificar si todos los checkboxes están marcados
  function updateSelectAllCheckbox() {
    const totalCheckboxes = productoCheckboxes.length;
    const checkedCount = Array.from(productoCheckboxes).filter(
      (checkbox) => checkbox.checked
    ).length;

    selectAllCheckbox.checked =
      totalCheckboxes > 0 && checkedCount === totalCheckboxes;
    selectAllCheckbox.indeterminate =
      checkedCount > 0 && checkedCount < totalCheckboxes;
  }

  // Agregar evento a cada checkbox individual
  productoCheckboxes.forEach((checkbox) => {
    checkbox.addEventListener("change", updateSelectAllCheckbox);
  });

  // Función para obtener productos seleccionados
  function getSelectedProducts() {
    return Array.from(productoCheckboxes)
      .filter((checkbox) => checkbox.checked)
      .map((checkbox) => checkbox.value);
  }

  // Función para agregar productos seleccionados como campos ocultos
  function addHiddenProductInputs(container, selectedProducts) {
    container.innerHTML = "";
    selectedProducts.forEach(function (productoId) {
      const input = document.createElement("input");
      input.type = "hidden";
      input.name = "productosSeleccionados";
      input.value = productoId;
      container.appendChild(input);
    });
  }

  // Manejar el botón "Aplicar Descuento"
  btnAplicarDescuento.addEventListener("click", function () {
    const productosSeleccionados = getSelectedProducts();

    if (productosSeleccionados.length === 0) {
      alert(
        "Debe seleccionar al menos un producto para aplicar el código de descuento."
      );
      return;
    }

    // Limpiar y agregar productos seleccionados
    const container = document.getElementById("productosSeleccionadosAplicar");
    addHiddenProductInputs(container, productosSeleccionados);

    // Configurar fechas por defecto
    const today = new Date();
    const endDate = new Date();
    endDate.setDate(today.getDate() + 30); // Por defecto, 30 días de descuento

    document.getElementById("cod_fecha_creacion").valueAsDate = today;
    document.getElementById("cod_fecha_expiracion").valueAsDate = endDate;

    // Mostrar el modal
    modalAplicarDescuento.show();
  });

  // Manejar el botón "Eliminar Descuento"
  btnEliminarDescuento.addEventListener("click", function () {
    const productosSeleccionados = getSelectedProducts();

    if (productosSeleccionados.length === 0) {
      alert(
        "Debe seleccionar al menos un producto para eliminar su código de descuento."
      );
      return;
    }

    // Verificar que los productos seleccionados tengan descuento
    const productosConDescuento = Array.from(productoCheckboxes).filter(
      (checkbox) =>
        checkbox.checked && checkbox.dataset.tieneDescuento === "true"
    );

    if (productosConDescuento.length === 0) {
      alert(
        "Ninguno de los productos seleccionados tiene códigos de descuento para eliminar."
      );
      return;
    }

    if (productosConDescuento.length !== productosSeleccionados.length) {
      const sinDescuento =
        productosSeleccionados.length - productosConDescuento.length;
      if (
        !confirm(
          `${sinDescuento} producto(s) seleccionado(s) no tienen código de descuento. ¿Desea continuar eliminando solo los que sí tienen códigos?`
        )
      ) {
        return;
      }
    }

    // Configurar mensaje del modal
    const mensaje =
      productosConDescuento.length === 1
        ? `¿Está seguro de que desea eliminar el código de descuento del producto seleccionado?`
        : `¿Está seguro de que desea eliminar los códigos de descuento de los ${productosConDescuento.length} productos seleccionados?`;

    document.getElementById("mensajeEliminar").textContent = mensaje;

    // Guardar productos para eliminación
    window.productosParaEliminar = productosConDescuento.map(
      (checkbox) => checkbox.value
    );

    // Mostrar modal de confirmación
    modalEliminarDescuento.show();
  });

  // Confirmar eliminación
  document
    .getElementById("btnConfirmarEliminar")
    .addEventListener("click", function () {
      if (
        window.productosParaEliminar &&
        window.productosParaEliminar.length > 0
      ) {
        // Agregar productos a eliminar como campos ocultos
        const container = document.getElementById(
          "productosSeleccionadosHidden"
        );
        addHiddenProductInputs(container, window.productosParaEliminar);

        // Configurar acción
        actionInput.value = "eliminar";

        // Cerrar modal y enviar formulario
        modalEliminarDescuento.hide();
        formProductos.submit();
      }
    });

  // Manejar el botón "Enviar Código"
  btnEnviarCodigo.addEventListener("click", function () {
    const productosSeleccionados = getSelectedProducts();

    if (productosSeleccionados.length === 0) {
      alert(
        "Debe seleccionar al menos un producto para enviar códigos de descuento."
      );
      return;
    }

    // Verificar que los productos seleccionados tengan descuento
    const productosConDescuento = Array.from(productoCheckboxes).filter(
      (checkbox) =>
        checkbox.checked && checkbox.dataset.tieneDescuento === "true"
    );

    if (productosConDescuento.length === 0) {
      alert(
        "Ninguno de los productos seleccionados tiene códigos de descuento para enviar."
      );
      return;
    }

    if (productosConDescuento.length !== productosSeleccionados.length) {
      const sinDescuento =
        productosSeleccionados.length - productosConDescuento.length;
      if (
        !confirm(
          `${sinDescuento} producto(s) seleccionado(s) no tienen código de descuento. ¿Desea continuar enviando solo los códigos de los productos que sí tienen descuento?`
        )
      ) {
        return;
      }
    }

    // Confirmar envío
    const mensaje =
      productosConDescuento.length === 1
        ? `¿Desea enviar el código de descuento del producto seleccionado a usuarios con más de 10 ventas?`
        : `¿Desea enviar los códigos de descuento de los ${productosConDescuento.length} productos seleccionados a usuarios con más de 10 ventas?`;

    if (confirm(mensaje)) {
      // Agregar productos para envío
      const container = document.getElementById("productosSeleccionadosHidden");
      addHiddenProductInputs(
        container,
        productosConDescuento.map((checkbox) => checkbox.value)
      );

      // Configurar acción
      actionInput.value = "enviar";

      // Enviar formulario
      formProductos.submit();
    }
  });

  // Validar fechas en el modal de aplicar descuento
  document
    .getElementById("cod_fecha_expiracion")
    .addEventListener("change", function () {
      const fechaInicio = document.getElementById("cod_fecha_creacion").value;
      const fechaFin = this.value;

      if (
        fechaInicio &&
        fechaFin &&
        new Date(fechaInicio) >= new Date(fechaFin)
      ) {
        alert(
          "La fecha de expiración debe ser posterior a la fecha de inicio."
        );
        this.value = "";
      }
    });

  document
    .getElementById("cod_fecha_creacion")
    .addEventListener("change", function () {
      const fechaInicio = this.value;
      const fechaFin = document.getElementById("cod_fecha_expiracion").value;

      if (
        fechaInicio &&
        fechaFin &&
        new Date(fechaInicio) >= new Date(fechaFin)
      ) {
        alert("La fecha de inicio debe ser anterior a la fecha de expiración.");
        document.getElementById("cod_fecha_expiracion").value = "";
      }
    });

  // Resetear modales cuando se cierren
  document
    .getElementById("modalAplicarDescuento")
    .addEventListener("hidden.bs.modal", function () {
      document.getElementById("formAplicarDescuento").reset();
      document.getElementById("productosSeleccionadosAplicar").innerHTML = "";
    });

  document
    .getElementById("modalEliminarDescuento")
    .addEventListener("hidden.bs.modal", function () {
      window.productosParaEliminar = null;
    });

  // Funcionalidad de scroll con tecla presionada (Ctrl + drag)
  let isScrollMode = false;
  let isDragging = false;
  let startY = 0;
  let scrollStartY = 0;

  // Activar modo scroll al presionar Ctrl
  document.addEventListener("keydown", function (e) {
    if (e.ctrlKey && !isScrollMode) {
      isScrollMode = true;
      tablaContainer.style.cursor = "grab";
    }
  });

  // Desactivar modo scroll al soltar Ctrl
  document.addEventListener("keyup", function (e) {
    if (!e.ctrlKey && isScrollMode) {
      isScrollMode = false;
      isDragging = false;
      tablaContainer.style.cursor = "default";
    }
  });

  // Eventos de mouse para el scroll manual
  tablaContainer.addEventListener("mousedown", function (e) {
    if (isScrollMode) {
      isDragging = true;
      startY = e.clientY;
      scrollStartY = tablaContainer.scrollTop;
      tablaContainer.style.cursor = "grabbing";
      e.preventDefault();
    }
  });

  document.addEventListener("mousemove", function (e) {
    if (isDragging && isScrollMode) {
      const deltaY = startY - e.clientY;
      tablaContainer.scrollTop = scrollStartY + deltaY;
      e.preventDefault();
    }
  });

  document.addEventListener("mouseup", function () {
    if (isDragging && isScrollMode) {
      isDragging = false;
      tablaContainer.style.cursor = "grab";
    }
  });

  // Evitar la selección de texto durante el arrastre
  tablaContainer.addEventListener("selectstart", function (e) {
    if (isScrollMode) {
      e.preventDefault();
    }
  });
});
