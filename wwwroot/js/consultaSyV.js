/**Función para buscar en tablas de servicios y ventas*/
function searchTable(searchTerm, tableSelector) {
    const rows = document.querySelectorAll(`${tableSelector} tbody tr`);
    let foundResults = false;

    const existingNoResults = document.querySelector('.no-results-message');
    if (existingNoResults) {
        existingNoResults.remove();
    }

    // Filtrar filas según el término de búsqueda
    rows.forEach(row => {
        if (row.querySelector('td[colspan]')) {
            return;
        }

        const text = row.textContent.toLowerCase();
        if (text.includes(searchTerm.toLowerCase())) {
            row.style.display = '';
            foundResults = true;
        } else {
            row.style.display = 'none';
        }
    });

    // Mostrar mensaje cuando no hay resultados
    if (!foundResults && searchTerm.trim() !== '') {
        const tableContainer = document.querySelector(`${tableSelector}`).parentElement;

        const noResultsMessage = document.createElement('div');
        noResultsMessage.className = 'no-results-message';
        noResultsMessage.innerHTML = `
            <div class="empty-state">
                <i class="fas fa-search-minus"></i>
                <p>No se encontraron resultados para "<strong>${searchTerm}</strong>"</p>
            </div>
        `;

        tableContainer.appendChild(noResultsMessage);
    }

    const emptyStateRow = document.querySelector(`${tableSelector} tbody tr td[colspan]`)?.parentNode;
    if (emptyStateRow) {
        emptyStateRow.style.display = 'none';
    }
}