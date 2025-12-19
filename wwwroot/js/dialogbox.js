/**
 * DialogBox Library - Reusable dialog box system
 * Types: informant, warning, error, list
 */

class DialogBox {
    constructor() {
        this.modal = null;
        this.backdrop = null;
        // Default icons (can be overridden with setIcons)
        this.icons = {
            informant: { html: '✓', color: '#667eea' },
            warning: { html: '!', color: '#f5576c' },
            error: { html: '✕', color: '#fa709a' },
            list: { html: '🔔', color: '#667eea' }
        };
        this.initializeDOM();
    }

    // Allow external code to override icons (partial merge)
    setIcons(icons) {
        this.icons = Object.assign({}, this.icons, icons || {});
    }

    initializeDOM() {
        // Create backdrop
        this.backdrop = document.createElement('div');
        this.backdrop.id = 'dialogBoxBackdrop';
        this.backdrop.style.cssText = `
            display: none;
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.5);
            z-index: 1040;
        `;
        document.body.appendChild(this.backdrop);

        // Create modal container
        this.modal = document.createElement('div');
        this.modal.id = 'dialogBoxModal';
        this.modal.style.cssText = `
            display: none;
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: white;
            border-radius: 16px;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
            z-index: 1050;
            min-width: 320px;
            max-width: 500px;
            animation: slideIn 0.3s ease-out;
        `;

        // Add animation styles
        if (!document.getElementById('dialogBoxStyles')) {
            const style = document.createElement('style');
            style.id = 'dialogBoxStyles';
            style.textContent = `
                @keyframes slideIn {
                    from {
                        opacity: 0;
                        transform: translate(-50%, -45%);
                    }
                    to {
                        opacity: 1;
                        transform: translate(-50%, -50%);
                    }
                }

                .dialog-header {
                    padding: 24px 24px 16px;
                    border-bottom: 1px solid #f0f0f0;
                    display: flex;
                    align-items: center;
                    gap: 12px;
                }

                .dialog-header-icon {
                    font-size: 24px;
                    min-width: 24px;
                }

                .dialog-header-title {
                    font-size: 18px;
                    font-weight: 600;
                    margin: 0;
                    color: #333;
                }

                .dialog-body {
                    padding: 20px 24px;
                    color: #555;
                    line-height: 1.5;
                }

                .dialog-list {
                    max-height: 400px;
                    overflow-y: auto;
                }

                .dialog-list-item {
                    padding: 12px;
                    border-bottom: 1px solid #f5f5f5;
                    display: flex;
                    justify-content: space-between;
                    align-items: start;
                }

                .dialog-list-item:last-child {
                    border-bottom: none;
                }

                .dialog-list-item-content {
                    flex: 1;
                }

                .dialog-list-item-subject {
                    font-weight: 600;
                    color: #333;
                    margin-bottom: 4px;
                }

                .dialog-list-item-description {
                    font-size: 13px;
                    color: #777;
                }

                .dialog-footer {
                    padding: 16px 24px 24px;
                    display: flex;
                    gap: 12px;
                    justify-content: flex-end;
                }

                .dialog-button {
                    padding: 10px 24px;
                    border: none;
                    border-radius: 8px;
                    font-size: 14px;
                    font-weight: 500;
                    cursor: pointer;
                    transition: all 0.2s;
                    min-width: 100px;
                }

                .dialog-button-primary {
                    background-color: #667eea;
                    color: white;
                }

                .dialog-button-primary:hover {
                    background-color: #5568d3;
                    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
                }

                .dialog-button-secondary {
                    background-color: #f5f5f5;
                    color: #333;
                }

                .dialog-button-secondary:hover {
                    background-color: #e8e8e8;
                }

                .dialog-informant .dialog-header {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                }

                .dialog-informant .dialog-header-title {
                    color: white;
                }

                .dialog-warning .dialog-header {
                    background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
                    color: white;
                }

                .dialog-warning .dialog-header-title {
                    color: white;
                }

                .dialog-error .dialog-header {
                    background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);
                    color: white;
                }

                .dialog-error .dialog-header-title {
                    color: white;
                }

                .dialog-list .dialog-header {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                }

                .dialog-list .dialog-header-title {
                    color: white;
                }

                .dialog-checkbox-area {
                    border-top: 1px solid #f0f0f0;
                }
            `;
            document.head.appendChild(style);
        }

        document.body.appendChild(this.modal);

        // Close on backdrop click
        this.backdrop.addEventListener('click', () => this.close());
    }

    show(config) {
        const { type = 'informant', title = '', message = '', buttons = [], items = [], checkbox = null } = config;

        this.modal.className = `dialog-${type}`;
        this.modal.innerHTML = '';

        // Header
        const header = document.createElement('div');
        header.className = 'dialog-header';
        
        const icon = document.createElement('div');
        icon.className = 'dialog-header-icon';
        
        // Use configured icons if present
        const iconCfg = (this.icons && this.icons[type]) ? this.icons[type] : null;
        if (iconCfg) {
            icon.innerHTML = iconCfg.html || '';
            if (iconCfg.color) icon.style.color = iconCfg.color;
        } else {
            // Fallback
            switch (type) {
                case 'informant':
                    icon.innerHTML = '✓';
                    icon.style.color = '#667eea';
                    break;
                case 'warning':
                    icon.innerHTML = '!';
                    icon.style.color = '#f5576c';
                    break;
                case 'error':
                    icon.innerHTML = '✕';
                    icon.style.color = '#fa709a';
                    break;
                case 'list':
                    icon.innerHTML = '🔔';
                    break;
            }
        }

        const titleEl = document.createElement('h3');
        titleEl.className = 'dialog-header-title';
        titleEl.textContent = title;

        header.appendChild(icon);
        header.appendChild(titleEl);
        this.modal.appendChild(header);

        // Body
        const body = document.createElement('div');
        body.className = 'dialog-body';

        if (type === 'list' && items.length > 0) {
            const listContainer = document.createElement('div');
            listContainer.className = 'dialog-list';

            items.forEach(item => {
                const listItem = document.createElement('div');
                listItem.className = 'dialog-list-item';

                const content = document.createElement('div');
                content.className = 'dialog-list-item-content';

                const subject = document.createElement('div');
                subject.className = 'dialog-list-item-subject';
                subject.textContent = item.subject;

                const description = document.createElement('div');
                description.className = 'dialog-list-item-description';
                description.textContent = item.description;

                content.appendChild(subject);
                content.appendChild(description);
                listItem.appendChild(content);

                if (item.action) {
                    const actionBtn = document.createElement('button');
                    actionBtn.className = 'dialog-button dialog-button-secondary';
                    actionBtn.textContent = '✕';
                    actionBtn.style.padding = '4px 8px';
                    actionBtn.style.minWidth = 'auto';
                    actionBtn.addEventListener('click', () => {
                        item.action();
                    });
                    listItem.appendChild(actionBtn);
                }

                listContainer.appendChild(listItem);
            });

            body.appendChild(listContainer);
        } else {
            body.textContent = message;
        }

        this.modal.appendChild(body);

        // Optional checkbox area (for confirmations that require an explicit checkbox)
        let checkboxEl = null;
        if (checkbox && checkbox.label) {
            const checkboxArea = document.createElement('div');
            checkboxArea.className = 'dialog-checkbox-area';
            checkboxArea.style.padding = '0 24px 12px';

            const cb = document.createElement('input');
            cb.type = 'checkbox';
            cb.id = 'dialog-confirm-checkbox';
            cb.checked = checkbox.checked || false;
            cb.style.marginRight = '8px';

            const lbl = document.createElement('label');
            lbl.htmlFor = cb.id;
            lbl.textContent = checkbox.label;
            lbl.style.fontSize = '14px';
            lbl.style.color = '#333';

            checkboxArea.appendChild(cb);
            checkboxArea.appendChild(lbl);
            this.modal.appendChild(checkboxArea);
            checkboxEl = cb;
        }

        // Footer
        const footer = document.createElement('div');
        footer.className = 'dialog-footer';

        const defaultButtons = {
            informant: [{ text: 'Tamam', action: () => this.close(), isPrimary: true }],
            warning: [
                { text: 'İptal', action: () => this.close(), isPrimary: false },
                { text: 'Evet', action: () => this.close(), isPrimary: true }
            ],
            error: [{ text: 'Tamam', action: () => this.close(), isPrimary: true }],
            list: [{ text: 'Kapat', action: () => this.close(), isPrimary: true }]
        };

        const buttonsToRender = buttons.length > 0 ? buttons : (defaultButtons[type] || []);

        buttonsToRender.forEach(btn => {
            const button = document.createElement('button');
            button.className = `dialog-button ${btn.isPrimary ? 'dialog-button-primary' : 'dialog-button-secondary'}`;
            button.textContent = btn.text;
            button.addEventListener('click', () => {
                if (btn.action) btn.action();
            });
            // If checkbox is required, disable primary buttons until checked
            if (checkboxEl && btn.isPrimary) {
                button.disabled = !checkboxEl.checked;
                checkboxEl.addEventListener('change', () => {
                    button.disabled = !checkboxEl.checked;
                });
            }
            footer.appendChild(button);
        });

        this.modal.appendChild(footer);

        // Show
        this.backdrop.style.display = 'block';
        this.modal.style.display = 'block';
    }

    close() {
        this.backdrop.style.display = 'none';
        this.modal.style.display = 'none';
    }
}

// Global instance
window.DialogBox = new DialogBox();
