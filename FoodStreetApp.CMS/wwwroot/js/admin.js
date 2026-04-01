(() => {
    function showLoading(show) {
        const overlay = document.getElementById('pageLoadingOverlay');
        if (!overlay) return;
        overlay.classList.toggle('d-none', !show);
    }

    function initLoadingState() {
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', () => showLoading(true));
        });
        window.addEventListener('pageshow', () => showLoading(false));
    }

    function initDeleteConfirmation() {
        const modalElement = document.getElementById('deleteConfirmModal');
        const confirmButton = document.getElementById('confirmDeleteBtn');
        if (!modalElement || !confirmButton || !window.bootstrap) return;

        const modal = new bootstrap.Modal(modalElement);
        let targetForm = null;

        document.addEventListener('click', (event) => {
            const deleteButton = event.target.closest('.js-delete-btn');
            if (!deleteButton) return;

            event.preventDefault();
            const formId = deleteButton.getAttribute('data-delete-form');
            targetForm = formId ? document.getElementById(formId) : null;
            if (targetForm) {
                modal.show();
            }
        });

        confirmButton.addEventListener('click', () => {
            if (targetForm) {
                targetForm.submit();
            }
        });
    }

    function initCrudModal() {
        document.addEventListener('click', (event) => {
            const button = event.target.closest('[data-modal-target]');
            if (!button) return;

            const modalId = button.getAttribute('data-modal-target');
            const modalElement = document.getElementById(modalId);
            if (!modalElement || !window.bootstrap) return;

            const form = modalElement.querySelector('form');
            if (!form) return;

            const mode = button.getAttribute('data-mode') || 'create';
            modalElement.querySelectorAll('[data-role="modal-title"]').forEach(el => {
                el.textContent = mode === 'edit' ? 'Edit Item' : 'Create Item';
            });

            form.querySelectorAll('[data-input]').forEach(input => {
                const key = input.getAttribute('data-input');
                input.value = button.getAttribute(`data-${key}`) ?? '';
            });

            const createButton = modalElement.querySelector('[data-role="create-submit"]');
            const editButton = modalElement.querySelector('[data-role="edit-submit"]');
            if (createButton && editButton) {
                createButton.classList.toggle('d-none', mode === 'edit');
                editButton.classList.toggle('d-none', mode !== 'edit');
            }

            const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
            modal.show();
        });
    }

    function initTableEnhancements() {
        document.querySelectorAll('[data-enhance-table]').forEach(table => {
            const tableId = table.id;
            if (!tableId) return;

            const tbody = table.querySelector('tbody');
            if (!tbody) return;

            const allRows = Array.from(tbody.querySelectorAll('tr'));
            const searchInput = document.querySelector(`[data-table-search="${tableId}"]`);
            const paginationHost = document.querySelector(`[data-table-pagination="${tableId}"]`);
            const pageSize = Number(table.getAttribute('data-page-size') || 8);
            let currentPage = 1;

            function getFilteredRows() {
                const query = (searchInput?.value || '').trim().toLowerCase();
                if (!query) return allRows;
                return allRows.filter(row => row.innerText.toLowerCase().includes(query));
            }

            function render() {
                const filteredRows = getFilteredRows();
                const pageCount = Math.max(1, Math.ceil(filteredRows.length / pageSize));
                currentPage = Math.min(currentPage, pageCount);

                allRows.forEach(row => row.classList.add('d-none'));
                filteredRows
                    .slice((currentPage - 1) * pageSize, currentPage * pageSize)
                    .forEach(row => row.classList.remove('d-none'));

                if (!paginationHost) return;
                paginationHost.innerHTML = '';

                const prev = document.createElement('button');
                prev.type = 'button';
                prev.className = 'btn btn-sm btn-outline-secondary';
                prev.innerHTML = '<i class="bi bi-chevron-left"></i>';
                prev.disabled = currentPage <= 1;
                prev.addEventListener('click', () => {
                    currentPage -= 1;
                    render();
                });
                paginationHost.appendChild(prev);

                const pageInfo = document.createElement('span');
                pageInfo.className = 'small text-secondary';
                pageInfo.textContent = `Page ${currentPage} of ${pageCount}`;
                paginationHost.appendChild(pageInfo);

                const next = document.createElement('button');
                next.type = 'button';
                next.className = 'btn btn-sm btn-outline-secondary';
                next.innerHTML = '<i class="bi bi-chevron-right"></i>';
                next.disabled = currentPage >= pageCount;
                next.addEventListener('click', () => {
                    currentPage += 1;
                    render();
                });
                paginationHost.appendChild(next);
            }

            if (searchInput) {
                searchInput.addEventListener('input', () => {
                    currentPage = 1;
                    render();
                });
            }

            render();
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        initLoadingState();
        initDeleteConfirmation();
        initCrudModal();
        initTableEnhancements();
    });
})();
