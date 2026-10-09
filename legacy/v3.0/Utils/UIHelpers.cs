using System;
using System.Drawing;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;

namespace GameOptimizer.Utils
{
    /// <summary>
    /// Вспомогательные функции для UI
    /// </summary>
    public static class UIHelpers
    {
        /// <summary>
        /// Применяет стиль кнопки с современным дизайном
        /// </summary>
        public static void ApplyModernButtonStyle(Button button, Color backgroundColor, int cornerRadius = 4)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backgroundColor;
            button.ForeColor = Theme.Current.Foreground;
            button.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Применяет стиль карточки с современным дизайном
        /// </summary>
        public static void ApplyCardStyle(Control card)
        {
            card.BackColor = Theme.Current.CardBackground;
            card.ForeColor = Theme.Current.Foreground;
            card.Font = new Font("Segoe UI", 9f);
        }

        /// <summary>
        /// Создаёт подсвеченную панель для выбранного элемента
        /// </summary>
        public static Panel CreateHighlightPanel()
        {
            return new Panel
            {
                BackColor = Theme.Current.Accent,
                Size = new Size(3, 0),
                Dock = DockStyle.Left
            };
        }

        /// <summary>
        /// Применяет стиль чекбокса
        /// </summary>
        public static void ApplyCheckBoxStyle(CheckBox checkBox)
        {
            checkBox.ForeColor = Theme.Current.Foreground;
            checkBox.Font = new Font("Segoe UI", 9f);
            checkBox.AutoSize = true;
        }

        /// <summary>
        /// Создаёт разделитель
        /// </summary>
        public static Panel CreateSeparator(int height = 1, int width = -1)
        {
            var separator = new Panel
            {
                BackColor = Theme.Current.BorderColor,
                Height = height,
                Dock = DockStyle.Top
            };
            return separator;
        }

        /// <summary>
        /// Показывает уведомление об успехе
        /// </summary>
        public static DialogResult ShowSuccessMessage(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Показывает уведомление об ошибке
        /// </summary>
        public static DialogResult ShowErrorMessage(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Показывает подтверждение
        /// </summary>
        public static DialogResult ShowConfirmation(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        /// <summary>
        /// Показывает уведомление об предупреждении
        /// </summary>
        public static DialogResult ShowWarning(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Получает цвет для статуса твика
        /// </summary>
        public static Color GetStatusColor(bool isApplied)
        {
            return isApplied ? Theme.Current.SuccessColor : Theme.Current.DisabledColor;
        }

        /// <summary>
        /// Получает текст для статуса
        /// </summary>
        public static string GetStatusText(bool isApplied)
        {
            return isApplied ? "Применён" : "Не применён";
        }

        /// <summary>
        /// Применяет стиль для текстбокса
        /// </summary>
        public static void ApplyTextBoxStyle(TextBox textBox)
        {
            textBox.BackColor = Theme.Current.CardBackground;
            textBox.ForeColor = Theme.Current.Foreground;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 9f);
        }

        /// <summary>
        /// Применяет стиль для комбобокса
        /// </summary>
        public static void ApplyComboBoxStyle(ComboBox comboBox)
        {
            comboBox.BackColor = Theme.Current.CardBackground;
            comboBox.ForeColor = Theme.Current.Foreground;
            comboBox.Font = new Font("Segoe UI", 9f);
        }

        /// <summary>
        /// Применяет стиль для DataGridView
        /// </summary>
        public static void ApplyDataGridViewStyle(DataGridView dataGridView)
        {
            dataGridView.BackgroundColor = Theme.Current.Background;
            dataGridView.ForeColor = Theme.Current.Foreground;
            dataGridView.GridColor = Theme.Current.BorderColor;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Theme.Current.CardBackground;
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Current.Foreground;
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Current.Accent;
            dataGridView.DefaultCellStyle.BackColor = Theme.Current.Background;
            dataGridView.DefaultCellStyle.ForeColor = Theme.Current.Foreground;
        }
    }
}
