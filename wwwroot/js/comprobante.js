document.addEventListener('DOMContentLoaded', function() {
    const boletaRadio = document.getElementById('boleta');
    const facturaRadio = document.getElementById('factura');
    const pagoTarjetaRadio = document.getElementById('pagoTarjeta');
    const pagoYapeRadio = document.getElementById('pagoYape');
    const botonStripe = document.getElementById('botonStripe');
    const botonYape = document.getElementById('botonYape');
    const camposBoleta = document.getElementById('camposBoleta');
    const camposFactura = document.getElementById('camposFactura');

    // Función para mostrar campos según tipo de comprobante
    function mostrarCamposComprobante() {
        if (boletaRadio.checked) {
            camposBoleta.style.display = 'flex';
            camposFactura.style.display = 'none';
        } else {
            camposBoleta.style.display = 'none';
            camposFactura.style.display = 'flex';
        }
    }

    // Función para actualizar botones de pago
    function actualizarBotonesPago() {
        if (pagoTarjetaRadio.checked) {
            botonStripe.style.display = 'block';
            botonYape.style.display = 'none';
        } else {
            botonStripe.style.display = 'none';
            botonYape.style.display = 'block';
        }
    }

    // Asignar event listeners
    boletaRadio.addEventListener('change', mostrarCamposComprobante);
    facturaRadio.addEventListener('change', mostrarCamposComprobante);
    pagoTarjetaRadio.addEventListener('change', actualizarBotonesPago);
    pagoYapeRadio.addEventListener('change', actualizarBotonesPago);
    
    // Inicializar estado de la página
    mostrarCamposComprobante();
    actualizarBotonesPago();
});