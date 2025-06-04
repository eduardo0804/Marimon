document.addEventListener("DOMContentLoaded", function () {
    const tipoEntidad = document.getElementById("tipoEntidad");
    const productoSection = document.getElementById("productoSection");
    const servicioSection = document.getElementById("servicioSection");
    const numeroComprobante = document.getElementById("numeroComprobante");
    const numeroReserva = document.getElementById("numeroReserva");
    const listaComprobantes = document.getElementById("listaComprobantes");
    const listaReservas = document.getElementById("listaReservas");
    const productoSelect = document.getElementById("productoSelect");
    const servicioSelect = document.getElementById("servicioSelect");
    const hiddenEntidadId = document.getElementById("hiddenEntidadId");
    const hiddenNombreEntidad = document.getElementById("hiddenNombreEntidad");
    const montoProducto = document.getElementById("montoProducto");
    const montoServicio = document.getElementById("montoServicio");
    const numeroReferencia = document.getElementById("numeroReferencia");
    const loadingComprobante = document.getElementById("loadingComprobante");
    const loadingReserva = document.getElementById("loadingReserva");

    let todosComprobantes = [];
    let todasReservas = [];
    let comprobanteSeleccionado = null;
    let reservaSeleccionada = null;
    let submitted = false;

    // Manejar cambio de tipo de entidad
    tipoEntidad.addEventListener("change", function () {
        const tipo = tipoEntidad.value;
        if (tipo === "1") { 
            productoSection.style.display = "block";
            servicioSection.style.display = "none";
            cargarComprobantes();
            setRequiredAttributes(true, false);
        } else if (tipo === "2") { 
            servicioSection.style.display = "block";
            productoSection.style.display = "none";
            cargarReservas();
            setRequiredAttributes(false, true);
        } else {
            productoSection.style.display = "none";
            servicioSection.style.display = "none";
            setRequiredAttributes(false, false);
        }

        limpiarSelecciones();
        limpiarValidaciones();
    });

    function setRequiredAttributes(producto, servicio) {
        if (numeroComprobante) numeroComprobante.toggleAttribute("required", producto);
        if (productoSelect) productoSelect.toggleAttribute("required", producto);
        if (montoProducto) montoProducto.toggleAttribute("required", producto);
        
        if (numeroReserva) numeroReserva.toggleAttribute("required", servicio);
        if (servicioSelect) servicioSelect.toggleAttribute("required", servicio);
        if (montoServicio) montoServicio.toggleAttribute("required", servicio);
    }

    // Cargar comprobantes del usuario
    async function cargarComprobantes() {
        try {
            loadingComprobante.style.display = "block";
            const usuarioId = document.querySelector('input[name="UsuarioId"]').value;
            const response = await fetch(`/Reclamacion/ObtenerComprobantesUsuario?usuarioId=${usuarioId}`);
            const data = await response.json();

            if (data.success) {
                todosComprobantes = data.comprobantes;
                mostrarListaComprobantes(todosComprobantes);
            }
        } catch (error) {
            console.error("Error al cargar comprobantes:", error);
        } finally {
            loadingComprobante.style.display = "none";
        }
    }

    // Cargar reservas del usuario
    async function cargarReservas() {
        try {
            loadingReserva.style.display = "block";
            const usuarioId = document.querySelector('input[name="UsuarioId"]').value;
            const response = await fetch(`/Reclamacion/ObtenerComprobantesUsuario?usuarioId=${usuarioId}`);
            const data = await response.json();

            if (data.success) {
                todasReservas = data.reservas;
                mostrarListaReservas(todasReservas);
            }
        } catch (error) {
            console.error("Error al cargar reservas:", error);
        } finally {
            loadingReserva.style.display = "none";
        }
    }

    // Mostrar lista filtrada de comprobantes
    function mostrarListaComprobantes(comprobantes) {
        listaComprobantes.innerHTML = "";
        
        if (comprobantes.length === 0) {
            listaComprobantes.innerHTML = '<div class="dropdown-item text-muted">No se encontraron comprobantes</div>';
        } else {
            comprobantes.forEach(comprobante => {
                const item = document.createElement("div");
                item.className = "dropdown-item cursor-pointer";
                item.innerHTML = `
                    <div class="d-flex justify-content-between">
                        <div>
                            <strong>#${comprobante.numero}</strong> - ${comprobante.tipo}
                            <br><small class="text-muted">${comprobante.fecha} - S/. ${comprobante.total.toFixed(2)}</small>
                        </div>
                        <small class="text-info">${comprobante.productos.length} producto(s)</small>
                    </div>
                `;
                
                item.addEventListener("click", () => seleccionarComprobante(comprobante));
                listaComprobantes.appendChild(item);
            });
        }
        
        listaComprobantes.style.display = "block";
    }

    // Mostrar lista filtrada de reservas
    function mostrarListaReservas(reservas) {
        listaReservas.innerHTML = "";
        
        if (reservas.length === 0) {
            listaReservas.innerHTML = '<div class="dropdown-item text-muted">No se encontraron reservas</div>';
        } else {
            reservas.forEach(reserva => {
                const item = document.createElement("div");
                item.className = "dropdown-item cursor-pointer";
                item.innerHTML = `
                    <div>
                        <strong>#${reserva.numero}</strong> - ${reserva.servicio.nombre}
                        <br><small class="text-muted">${reserva.fecha} ${reserva.hora} - ${reserva.estado}</small>
                    </div>
                `;
                
                item.addEventListener("click", () => seleccionarReserva(reserva));
                listaReservas.appendChild(item);
            });
        }
        
        listaReservas.style.display = "block";
    }

    // Filtrar comprobantes mientras escribe
    if (numeroComprobante) {
        numeroComprobante.addEventListener("input", function() {
            const filtro = this.value.toLowerCase();
            const comprobantesFiltrados = todosComprobantes.filter(c => 
                c.numero.toString().includes(filtro) ||
                c.tipo.toLowerCase().includes(filtro) ||
                c.fecha.includes(filtro)
            );
            mostrarListaComprobantes(comprobantesFiltrados);
        });

        numeroComprobante.addEventListener("focus", function() {
            if (todosComprobantes.length > 0) {
                mostrarListaComprobantes(todosComprobantes);
            }
        });
    }

    // Filtrar reservas mientras escribe
    if (numeroReserva) {
        numeroReserva.addEventListener("input", function() {
            const filtro = this.value.toLowerCase();
            const reservasFiltradas = todasReservas.filter(r => 
                r.numero.toString().includes(filtro) ||
                r.servicio.nombre.toLowerCase().includes(filtro) ||
                r.fecha.includes(filtro)
            );
            mostrarListaReservas(reservasFiltradas);
        });

        numeroReserva.addEventListener("focus", function() {
            if (todasReservas.length > 0) {
                mostrarListaReservas(todasReservas);
            }
        });
    }

    // Seleccionar comprobante
    function seleccionarComprobante(comprobante) {
        comprobanteSeleccionado = comprobante;
        numeroComprobante.value = comprobante.numero;
        listaComprobantes.style.display = "none";
        
        // Mostrar info del comprobante seleccionado
        document.getElementById("infoComprobanteSeleccionado").textContent = 
            `#${comprobante.numero} - ${comprobante.tipo} (${comprobante.fecha}) - S/. ${comprobante.total.toFixed(2)}`;
        document.getElementById("comprobanteSeleccionado").style.display = "block";
        
        // Cargar productos
        cargarProductosDelComprobante(comprobante);
        
        // Habilitar select de productos
        productoSelect.disabled = false;
    }

    // Seleccionar reserva
    function seleccionarReserva(reserva) {
        reservaSeleccionada = reserva;
        numeroReserva.value = reserva.numero;
        listaReservas.style.display = "none";
        
        // Mostrar info de la reserva seleccionada
        document.getElementById("infoReservaSeleccionada").textContent = 
            `#${reserva.numero} - ${reserva.servicio.nombre} (${reserva.fecha} ${reserva.hora})`;
        document.getElementById("reservaSeleccionada").style.display = "block";
        
        // Cargar servicio
        cargarServicioDeLaReserva(reserva);
        
        // Habilitar select de servicios
        servicioSelect.disabled = false;
    }

    // Cargar productos del comprobante seleccionado
    function cargarProductosDelComprobante(comprobante) {
        productoSelect.innerHTML = '<option value="">-- Seleccione Producto --</option>';
        
        comprobante.productos.forEach(producto => {
            const option = document.createElement("option");
            option.value = producto.id;
            option.textContent = `${producto.nombre} - S/. ${producto.montoTotal.toFixed(2)} (${producto.categoria})`;
            option.dataset.monto = producto.montoTotal;
            option.dataset.nombre = producto.nombre;
            productoSelect.appendChild(option);
        });
    }

    // Cargar servicio de la reserva seleccionada
    function cargarServicioDeLaReserva(reserva) {
        servicioSelect.innerHTML = '';
        
        const option = document.createElement("option");
        option.value = reserva.servicio.id;
        option.textContent = reserva.servicio.nombre;
        option.dataset.nombre = reserva.servicio.nombre;
        option.selected = true;
        servicioSelect.appendChild(option);
        
        // Auto-seleccionar y configurar
        hiddenEntidadId.value = reserva.servicio.id;
        hiddenNombreEntidad.value = reserva.servicio.nombre;
        numeroReferencia.value = reserva.numero;
        montoServicio.value = "50.00";
        document.getElementById("montoInfoServicio").textContent = "Precio sugerido: S/. 50.00 (puedes cambiarlo)";
    }

    // Manejar selección de producto
    if (productoSelect) {
        productoSelect.addEventListener("change", function() {
            if (this.value) {
                const selectedOption = this.options[this.selectedIndex];
                const monto = selectedOption.dataset.monto;
                const nombre = selectedOption.dataset.nombre;
                
                montoProducto.value = parseFloat(monto).toFixed(2);
                montoProducto.max = monto;
                document.getElementById("montoInfoProducto").textContent = `Monto máximo a reclamar: S/. ${parseFloat(monto).toFixed(2)}`;
                
                hiddenEntidadId.value = this.value;
                hiddenNombreEntidad.value = nombre;
                numeroReferencia.value = comprobanteSeleccionado.numero;
            } else {
                limpiarSeleccionesProducto();
            }
        });
    }

    // Ocultar listas al hacer click fuera
    document.addEventListener("click", function(event) {
        if (!event.target.closest("#numeroComprobante") && !event.target.closest("#listaComprobantes")) {
            listaComprobantes.style.display = "none";
        }
        if (!event.target.closest("#numeroReserva") && !event.target.closest("#listaReservas")) {
            listaReservas.style.display = "none";
        }
    });

    function limpiarSelecciones() {
        comprobanteSeleccionado = null;
        reservaSeleccionada = null;
        hiddenEntidadId.value = "";
        hiddenNombreEntidad.value = "";
        numeroReferencia.value = "";
        
        if (numeroComprobante) numeroComprobante.value = "";
        if (numeroReserva) numeroReserva.value = "";
        if (montoProducto) montoProducto.value = "";
        if (montoServicio) montoServicio.value = "";
        
        // Ocultar alertas de selección
        document.getElementById("comprobanteSeleccionado").style.display = "none";
        document.getElementById("reservaSeleccionada").style.display = "none";
        
        // Deshabilitar selects
        if (productoSelect) {
            productoSelect.disabled = true;
            productoSelect.innerHTML = '<option value="">-- Primero selecciona un comprobante --</option>';
        }
        if (servicioSelect) {
            servicioSelect.disabled = true;
            servicioSelect.innerHTML = '<option value="">-- Primero selecciona una reserva --</option>';
        }
        
        // Restaurar textos de ayuda
        if (document.getElementById("montoInfoProducto")) {
            document.getElementById("montoInfoProducto").textContent = "El monto se llenará automáticamente";
        }
        if (document.getElementById("montoInfoServicio")) {
            document.getElementById("montoInfoServicio").textContent = "Ingresa el monto que pagaste por este servicio";
        }
    }

    function limpiarSeleccionesProducto() {
        hiddenEntidadId.value = "";
        hiddenNombreEntidad.value = "";
        if (montoProducto) montoProducto.value = "";
        document.getElementById("montoInfoProducto").textContent = "El monto se llenará automáticamente";
    }

    function limpiarValidaciones() {
        [tipoEntidad, numeroComprobante, numeroReserva, productoSelect, servicioSelect,
         montoProducto, montoServicio, document.querySelector('#tipoReclamacion'),
         document.querySelector('#descripcion')]
            .forEach(el => {
                if (el) {
                    el.classList.remove("is-invalid", "is-valid");
                }
            });
    }

    function validarCampo(campo) {
        if (!campo) return true;
        if (!campo.value || campo.value.trim() === "") {
            campo.classList.add("is-invalid");
            campo.classList.remove("is-valid");
            return false;
        } else {
            campo.classList.remove("is-invalid");
            campo.classList.add("is-valid");
            return true;
        }
    }

    function validarCamposVisibles() {
        let valido = true;

        if (!validarCampo(document.querySelector('#tipoReclamacion'))) valido = false;
        if (!validarCampo(tipoEntidad)) valido = false;

        if (productoSection.style.display === 'block') {
            if (!comprobanteSeleccionado) {
                numeroComprobante.classList.add("is-invalid");
                valido = false;
            }
            if (!validarCampo(productoSelect)) valido = false;
            if (!validarCampo(montoProducto)) valido = false;
        } else if (servicioSection.style.display === 'block') {
            if (!reservaSeleccionada) {
                numeroReserva.classList.add("is-invalid");
                valido = false;
            }
            if (!validarCampo(servicioSelect)) valido = false;
            if (!validarCampo(montoServicio)) valido = false;
        }

        if (!validarCampo(document.querySelector('#descripcion'))) valido = false;

        return valido;
    }

    // Envío del formulario
    document.getElementById('reclamacionForm').addEventListener('submit', function (event) {
        submitted = true;
        limpiarValidaciones();

        if (!validarCamposVisibles()) {
            event.preventDefault();
            event.stopPropagation();
            return false;
        }

        if (!hiddenEntidadId.value) {
            alert("Por favor, selecciona un producto o servicio.");
            event.preventDefault();
            return false;
        }

        console.log("Enviando:", {
            EntidadId: hiddenEntidadId.value,
            NombreEntidad: hiddenNombreEntidad.value,
            NumeroReferencia: numeroReferencia.value,
            TipoEntidad: tipoEntidad.value
        });
    });

    // Event listeners para validación
    [tipoEntidad, numeroComprobante, numeroReserva, productoSelect, servicioSelect,
     montoProducto, montoServicio, document.querySelector('#tipoReclamacion'),
     document.querySelector('#descripcion')]
        .forEach(el => {
            if (!el) return;
            el.addEventListener('blur', function () {
                if (!submitted) return;
                validarCampo(el);
            });
            el.addEventListener('input', function () {
                if (!submitted) return;
                validarCampo(el);
            });
        });
});