class EncuestaSatisfaccion {
    constructor() {
        this.currentStep = 1;
        this.totalSteps = 3;
        this.formData = {};
        this.init();
    }

    init() {
        console.log('Inicializando Encuesta de Satisfacción');
        this.bindEvents();
        this.updateProgress();
    }

    bindEvents() {
        $(document).on('click', '#openSurveyModal', () => {
            this.openModal();
        });

        $(document).on('click', '#closeSurveyModal, .modal-overlay', (e) => {
            if (e.target === e.currentTarget) {
                this.closeModal();
            }
        });

        $(document).on('click', '.modal-content', (e) => {
            e.stopPropagation();
        });

        $(document).on('keydown', (e) => {
            if (e.key === 'Escape' && $('#surveyModal').hasClass('show')) {
                this.closeModal();
            }
        });

        $(document).on('click', '#nextBtn', () => {
            this.nextStep();
        });

        $(document).on('click', '#prevBtn', () => {
            this.prevStep();
        });

        $(document).on('click', '.rating-option', (e) => {
            this.selectRatingOption(e.currentTarget);
        });

        $(document).on('click', '.nps-option', (e) => {
            this.selectNpsOption(e.currentTarget);
        });

        $(document).on('submit', '#satisfactionSurvey', (e) => {
            e.preventDefault();
            this.submitSurvey();
        });

        $(document).on('input', '.form-input, .comment-area', (e) => {
            this.clearFieldError(e.target);
        });
    }

    openModal() {
        console.log('Abriendo modal de encuesta');
        $('#surveyModal').addClass('show');
        $('body').css('overflow', 'hidden');
        
        this.resetForm();
        
        setTimeout(() => {
            $('.modal-content').addClass('animate-in');
        }, 100);
    }

    closeModal() {
        console.log('Cerrando modal de encuesta');
        $('#surveyModal').removeClass('show');
        $('body').css('overflow', 'auto');
        
        setTimeout(() => {
            this.resetForm();
        }, 300);
    }

    resetForm() {
        this.currentStep = 1;
        this.formData = {};
        
        $('#satisfactionSurvey')[0].reset();
        
        $('.rating-option, .nps-option').removeClass('selected');
        
        this.showStep(1);
        
        this.clearAllErrors();
        
        $('#surveyThankYou').removeClass('show');
        $('.survey-form').show();
    }

    nextStep() {
        console.log(`Avanzando del paso ${this.currentStep}`);
        
        if (this.validateCurrentStep()) {
            this.saveCurrentStepData();
            
            if (this.currentStep < this.totalSteps) {
                this.currentStep++;
                this.showStep(this.currentStep);
                this.updateProgress();
            }
        }
    }

    prevStep() {
        console.log(`⬅Retrocediendo del paso ${this.currentStep}`);
        
        if (this.currentStep > 1) {
            this.currentStep--;
            this.showStep(this.currentStep);
            this.updateProgress();
        }
    }

    showStep(stepNumber) {
        $('.survey-step').removeClass('active');
        
        setTimeout(() => {
            $(`#step${stepNumber}`).addClass('active');
        }, 150);

        this.updateNavigationButtons();
        
        $('.modal-content').scrollTop(0);
    }

    updateNavigationButtons() {
        const prevBtn = $('#prevBtn');
        const nextBtn = $('#nextBtn');
        const submitBtn = $('#submitBtn');

        // Botón anterior
        if (this.currentStep === 1) {
            prevBtn.hide();
        } else {
            prevBtn.show();
        }

        // Botón siguiente/enviar
        if (this.currentStep === this.totalSteps) {
            nextBtn.hide();
            submitBtn.show();
        } else {
            nextBtn.show();
            submitBtn.hide();
        }
    }

    updateProgress() {
        const percentage = (this.currentStep / this.totalSteps) * 100;
        $('#surveyProgress').css('width', `${percentage}%`);
        $('#currentStep').text(this.currentStep);
    }

    validateCurrentStep() {
        console.log(`Validando paso ${this.currentStep}`);
        
        const currentStepElement = $(`#step${this.currentStep}`);
        const requiredFields = currentStepElement.find('[required]');
        let isValid = true;

        this.clearAllErrors();

        requiredFields.each((index, field) => {
            const $field = $(field);
            
            if (field.type === 'radio') {
                const groupName = field.name;
                const isGroupValid = currentStepElement.find(`input[name="${groupName}"]:checked`).length > 0;
                
                if (!isGroupValid) {
                    isValid = false;
                    this.showFieldError($field.closest('.form-group'), 'Por favor selecciona una opción');
                }
            } else if (field.type === 'email') {
                if (!field.value.trim() || !this.isValidEmail(field.value)) {
                    isValid = false;
                    this.showFieldError($field, 'Por favor ingresa un email válido');
                }
            } else {
                if (!field.value.trim()) {
                    isValid = false;
                    this.showFieldError($field, 'Este campo es obligatorio');
                }
            }
        });

        if (!isValid) {
            this.showNotification('Por favor completa todos los campos obligatorios', 'warning');
        }

        return isValid;
    }

    saveCurrentStepData() {
        const currentStepElement = $(`#step${this.currentStep}`);
        const inputs = currentStepElement.find('input, textarea, select');

        inputs.each((index, input) => {
            const $input = $(input);
            
            if (input.type === 'radio') {
                if (input.checked) {
                    this.formData[input.name] = input.value;
                }
            } else {
                this.formData[input.name] = input.value;
            }
        });

        console.log('💾 Datos guardados:', this.formData);
    }

    selectRatingOption(option) {
        const $option = $(option);
        const groupName = $option.find('input[type="radio"]').attr('name');
        
        // Remover selección anterior del grupo
        $(`.rating-option input[name="${groupName}"]`).closest('.rating-option').removeClass('selected');
        
        // Seleccionar opción actual
        $option.addClass('selected');
        $option.find('input[type="radio"]').prop('checked', true);
        
        // Limpiar error si existe
        this.clearFieldError($option.closest('.form-group'));
        
        console.log(`⭐ Seleccionado ${groupName}: ${$option.find('input').val()}`);
    }

    selectNpsOption(option) {
        const $option = $(option);
        
        // Remover selección anterior
        $('.nps-option').removeClass('selected');
        
        // Seleccionar opción actual
        $option.addClass('selected');
        $option.find('input[type="radio"]').prop('checked', true);
        
        // Limpiar error si existe
        this.clearFieldError($option.closest('.form-group'));
        
        console.log(`📊 NPS seleccionado: ${$option.find('input').val()}`);
    }

    async submitSurvey() {
        console.log('📤 Enviando encuesta');
        
        if (!this.validateCurrentStep()) {
            return;
        }

        this.saveCurrentStepData();

        const submitBtn = $('#submitBtn');
        const originalText = submitBtn.html();
        submitBtn.html('<i class="fas fa-spinner fa-spin"></i> Enviando...').prop('disabled', true);

        try {
            const response = await this.sendSurveyData(this.formData);
            
            if (response.success) {
                this.showThankYou();
                console.log('Encuesta enviada exitosamente');
            } else {
                throw new Error(response.message || 'Error al enviar la encuesta');
            }
        } catch (error) {
            console.error('Error al enviar encuesta:', error);
            this.showNotification('Error al enviar la encuesta. Por favor intenta nuevamente.', 'error');
        } finally {
            submitBtn.html(originalText).prop('disabled', false);
        }
    }

    async sendSurveyData(data) {
        // Simular envío AJAX - Reemplazar con tu endpoint real
        return new Promise((resolve) => {
            setTimeout(() => {
                console.log('📊 Datos enviados:', data);
                resolve({ success: true, message: 'Encuesta guardada correctamente' });
            }, 2000);
        });

        /* Implementación real con AJAX:
        return $.ajax({
            url: '/api/encuesta-satisfaccion',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        });
        */
    }

    showThankYou() {
        $('.survey-form').hide();
        $('#surveyThankYou').addClass('show');
        
        // Cerrar modal automáticamente después de 5 segundos
        setTimeout(() => {
            this.closeModal();
        }, 5000);
        
        // Confetti celebration
        if (typeof confetti !== 'undefined') {
            confetti({
                particleCount: 100,
                spread: 70,
                origin: { y: 0.6 },
                colors: ['#667eea', '#764ba2', '#ff6b6b', '#4ecdc4']
            });
        }
    }

    showFieldError(element, message) {
        const $element = $(element);
        
        if ($element.hasClass('form-group')) {
            $element.addClass('invalid-group');
        } else {
            $element.addClass('invalid');
        }
        
        // Mostrar tooltip de error si no existe
        if (!$element.find('.error-tooltip').length) {
            $element.append(`<div class="error-tooltip">${message}</div>`);
        }
    }

    clearFieldError(element) {
        const $element = $(element);
        $element.removeClass('invalid');
        $element.closest('.form-group').removeClass('invalid-group');
        $element.find('.error-tooltip').remove();
    }

    clearAllErrors() {
        $('.invalid, .invalid-group').removeClass('invalid invalid-group');
        $('.error-tooltip').remove();
    }

    showNotification(message, type = 'info') {
        // Crear notificación toast
        const notification = $(`
            <div class="survey-notification ${type}">
                <i class="fas fa-${type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
                <span>${message}</span>
            </div>
        `);

        $('body').append(notification);
        
        setTimeout(() => {
            notification.addClass('show');
        }, 100);

        setTimeout(() => {
            notification.removeClass('show');
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    // Método público para mostrar el modal con delay
    mostrar(delay = 0) {
        setTimeout(() => {
            this.openModal();
        }, delay);
    }
}

// Inicializar cuando el DOM esté listo
$(document).ready(function() {
    console.log('DOM listo - Inicializando encuesta');
    window.encuestaSatisfaccion = new EncuestaSatisfaccion();
});

const notificationStyles = `
<style>
.survey-notification {
    position: fixed;
    top: 20px;
    right: 20px;
    background: white;
    padding: 1rem 1.5rem;
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    display: flex;
    align-items: center;
    gap: 0.5rem;
    z-index: 10000;
    transform: translateX(100%);
    transition: transform 0.3s ease;
    max-width: 350px;
}

.survey-notification.show {
    transform: translateX(0);
}

.survey-notification.warning {
    border-left: 4px solid #f39c12;
}

.survey-notification.error {
    border-left: 4px solid #e74c3c;
}

.survey-notification.info {
    border-left: 4px solid #3498db;
}

.error-tooltip {
    position: absolute;
    bottom: -25px;
    left: 0;
    background: #e74c3c;
    color: white;
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
    font-size: 0.8rem;
    z-index: 1000;
}
</style>
`;

$('head').append(notificationStyles);