using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WFInfo
{
    public static class NumericOnlyEntry
    {
        public static string GetText(TextBox textBox)
        {
            return (string)textBox.GetValue(RegexFilterProperty);
        }

        public static void SetRegexFilter(TextBox textBox, string value)
        {
            textBox.SetValue(RegexFilterProperty, value);
        }

        public static readonly DependencyProperty RegexFilterProperty =
            DependencyProperty.RegisterAttached(
                "RegexFilter",
                typeof(string),
                typeof(NumericOnlyEntry),
                new UIPropertyMetadata(null, OnRegexFilterChanged));

        private static void OnRegexFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if(element == null) return;

            if (e.NewValue is string)
            {
                element.PreviewTextInput += PreviewTextInputHandler;
                element.PreviewKeyDown += PreviewKeyDownHandler;
            }
            else
            {
                element.PreviewTextInput -= PreviewTextInputHandler;
                element.PreviewKeyDown -= PreviewKeyDownHandler;
            }

        }

        static void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
        {
            string text;
            var textBox = sender as TextBox;
            if (textBox.Text.Length < textBox.CaretIndex)
                text = textBox.Text;
            else
            {
                //  Remaining text after removing selected text.
                string remainingTextAfterRemoveSelection;

                text = TreatSelectedText(textBox, out remainingTextAfterRemoveSelection)
                    ? remainingTextAfterRemoveSelection.Insert(textBox.SelectionStart, e.Text)
                    : textBox.Text.Insert(textBox.CaretIndex, e.Text);
            }

            e.Handled = !ValidateText(GetText(textBox) ,text);
        }

        private static string _emptyValue = "";
        static void PreviewKeyDownHandler(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrEmpty(_emptyValue))
                return;

            string text = null;

            // Handle the Backspace key
            if (e.Key == Key.Back)
            {
                if (!TreatSelectedText(textBox, out text))
                {
                    if (textBox.SelectionStart > 0)
                        text = textBox.Text.Remove(textBox.SelectionStart - 1, 1);
                }
            }
            // Handle the Delete key
            else if (e.Key == Key.Delete)
            {
                // If text was selected, delete it
                if (!TreatSelectedText(textBox, out text) && textBox.Text.Length > textBox.SelectionStart)
                {
                    // Otherwise delete next symbol
                    text = textBox.Text.Remove(textBox.SelectionStart, 1);
                }
            }

            if (text == string.Empty)
            {
                textBox.Text = _emptyValue;
                if (e.Key == Key.Back)
                    textBox.SelectionStart++;
                e.Handled = true;
            }
        }
        /// <summary>
        ///     Handle text selection
        /// </summary>
        /// <returns>true if the character was successfully removed; otherwise, false. </returns>
        private static bool TreatSelectedText(TextBox textBox, out string text)
        {
            text = null;
            if (textBox.SelectionLength <= 0) 
                return false;

            var length = textBox.Text.Length;
            if (textBox.SelectionStart >= length)
                return true;

            if (textBox.SelectionStart + textBox.SelectionLength >= length)
                textBox.SelectionLength = length - textBox.SelectionStart;

            text = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            return true;
        }

        // private static string _regex = "[0-9.]";
        private static int _maxLength = 10;
        /// <summary>
        ///     Validate certain text by our regular expression and text length conditions
        /// </summary>
        /// <param name="text"> Text for validation </param>
        /// <returns> True - valid, False - invalid </returns>
        private static bool ValidateText(string regex, string text)
        {
            return (new Regex(regex, RegexOptions.IgnoreCase)).IsMatch(text) && (_maxLength == int.MinValue || text.Length <= _maxLength);
        }
 
    }
}