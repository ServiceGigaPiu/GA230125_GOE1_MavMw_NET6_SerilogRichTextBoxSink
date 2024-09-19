#region Copyright 2022 Simon Vonhoff & Contributors

//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

using Serilog.Events;
using Serilog.Sinks.RichTextBoxForms.Themes;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Serilog.Sinks.RichTextBoxForms.Formatting {
	internal class IndentedJsonValueFormatter : ValueFormatter {
		private readonly DisplayValueFormatter _displayFormatter;

		public IndentedJsonValueFormatter(Theme theme, IFormatProvider? formatProvider) : base(theme) {
			_displayFormatter = new DisplayValueFormatter(theme, formatProvider);
		}

		protected override bool VisitScalarValue(ValueFormatterState state, ScalarValue scalar) {
			if (scalar is null) {
				throw new ArgumentNullException(nameof(scalar));
			}

			// At the top level, for scalar values, use "display" rendering.
			if (state.IsTopLevel) {
				_displayFormatter.FormatLiteralValue(scalar, state.RichTextBox, state.Format);
				return true;
			}

			FormatLiteralValue(scalar, state.RichTextBox);
			return true;
		}

		protected override bool VisitSequenceValue(ValueFormatterState state, SequenceValue sequence) {
			if (sequence is null) {
				throw new ArgumentNullException(nameof(sequence));
			}

			state = state.ToIndentUp();
			if (sequence.Elements.Count > 0)
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, "[\n" + GetIndentation(state));
			else
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, "[" + GetIndentation(state));

			var delimiter = string.Empty;
			foreach (var propertyValue in sequence.Elements) {
				if (!string.IsNullOrEmpty(delimiter)) {
					Theme.Render(state.RichTextBox, StyleToken.TertiaryText, delimiter);
				}

				delimiter = ",\n" + GetIndentation(state);
				Visit(state, propertyValue);
			}

			state.ToIndentDown();
			Theme.Render(state.RichTextBox, StyleToken.TertiaryText, $"\n{GetIndentation(state.ToIndentDown())}]");
			return true;
		}

		protected string GetIndentation(ValueFormatterState state) {
			return state.UseSpacesInsteadOfTabs ? "".PadLeft(state.IndentLvl * state.SingleIndentSpaceCount, ' ') : "".PadLeft(state.IndentLvl, '\t');
		}

		protected override bool VisitStructureValue(ValueFormatterState state, StructureValue structure) {
			state = state.ToIndentUp();
			if (structure.Properties.Count > 0)
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, "{\n" + GetIndentation(state));
			else
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, "{" + GetIndentation(state));

			var delimiter = string.Empty;
			foreach (var eventProperty in structure.Properties) {
				if (!string.IsNullOrEmpty(delimiter)) {
					Theme.Render(state.RichTextBox, StyleToken.TertiaryText, delimiter);
				}

				delimiter = ",\n" + GetIndentation(state);

				Theme.Render(state.RichTextBox, StyleToken.Name, GetQuotedJsonString(eventProperty.Name));
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, ": ");
				Visit(state.Next(), eventProperty.Value);
			}

			if (structure.TypeTag != null) {
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, delimiter);
				Theme.Render(state.RichTextBox, StyleToken.Name, GetQuotedJsonString("$type"));
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, ": ");
				Theme.Render(state.RichTextBox, StyleToken.String, GetQuotedJsonString(structure.TypeTag));
			}

			state = state.ToIndentDown();
			Theme.Render(state.RichTextBox, StyleToken.TertiaryText, $"\n{GetIndentation(state)}}}");
			return true;
		}

		protected override bool VisitDictionaryValue(ValueFormatterState state, DictionaryValue dictionary) {
			state = state.ToIndentUp();
			if (dictionary.Elements.Count > 0)
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, "{\n" + GetIndentation(state));
			else
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, "{" + GetIndentation(state));

			var delimiter = string.Empty;
			foreach (var (scalar, propertyValue) in dictionary.Elements) {
				if (!string.IsNullOrEmpty(delimiter)) {
					Theme.Render(state.RichTextBox, StyleToken.TertiaryText, delimiter);
				}

				delimiter = ",\n" + GetIndentation(state);

				var style = scalar.Value switch {
					null => StyleToken.Null,
					string => StyleToken.String,
					_ => StyleToken.Scalar
				};

				Theme.Render(state.RichTextBox, style, GetQuotedJsonString(scalar.Value?.ToString() ?? "null"));
				Theme.Render(state.RichTextBox, StyleToken.TertiaryText, ": ");

				Visit(state.Next(), propertyValue);
			}

			state = state.ToIndentDown();
			Theme.Render(state.RichTextBox, StyleToken.TertiaryText, $"\n{GetIndentation(state)}}}");
			return true;
		}

		private void FormatLiteralValue(ScalarValue scalar, RichTextBox richTextBox) {
			var value = scalar.Value;

			switch (value) {
				case null: {
					Theme.Render(richTextBox, StyleToken.Null, "null");
					return;
				}
				case string str: {
					Theme.Render(richTextBox, StyleToken.String, GetQuotedJsonString(str));
					return;
				}
				case ValueType and (int or uint or long or ulong or decimal or byte or sbyte or short or ushort): {
					Theme.Render(richTextBox, StyleToken.Number,
						 ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
					return;
				}
				case double d: {
					if (double.IsNaN(d) || double.IsInfinity(d)) {
						Theme.Render(richTextBox, StyleToken.Number,
							 GetQuotedJsonString(d.ToString(CultureInfo.InvariantCulture)));
					}
					else {
						Theme.Render(richTextBox, StyleToken.Number, d.ToString("R", CultureInfo.InvariantCulture));
					}

					return;
				}
				case float f: {
					if (float.IsNaN(f) || float.IsInfinity(f)) {
						Theme.Render(richTextBox, StyleToken.Number,
							 GetQuotedJsonString(f.ToString(CultureInfo.InvariantCulture)));
					}
					else {
						Theme.Render(richTextBox, StyleToken.Number, f.ToString("R", CultureInfo.InvariantCulture));
					}

					return;
				}
				case bool b: {
					Theme.Render(richTextBox, StyleToken.Boolean, b ? "true" : "false");
					return;
				}
				case char ch: {
					Theme.Render(richTextBox, StyleToken.Scalar,
						 GetQuotedJsonString(ch.ToString()));
					return;
				}
				case ValueType and (DateTime or DateTimeOffset): {
					Theme.Render(richTextBox, StyleToken.Scalar,
						 $"\"{((IFormattable)value).ToString("O", CultureInfo.InvariantCulture)}\"");
					return;
				}
				default: {
					Theme.Render(richTextBox, StyleToken.Scalar, GetQuotedJsonString(value.ToString() ?? string.Empty));
					break;
				}
			}
		}

		/// <summary>
		/// Write a valid JSON string literal, escaping as necessary.
		/// </summary>
		/// <param name="str">The string value to write.</param>
		public static string GetQuotedJsonString(string str) {
			var output = new StringWriter();
			output.Write('\"');

			var cleanSegmentStart = 0;
			var anyEscaped = false;

			for (var i = 0; i < str.Length; ++i) {
				var c = str[i];
				if (c is < (char)32 or '\\' or '"') {
					anyEscaped = true;

					output.Write(str[cleanSegmentStart..i]);
					cleanSegmentStart = i + 1;

					switch (c) {
						case '"': {
							output.Write("\\\"");
							break;
						}
						case '\\': {
							output.Write("\\\\");
							break;
						}
						case '\n': {
							output.Write("\\n");
							break;
						}
						case '\r': {
							output.Write("\\r");
							break;
						}
						case '\f': {
							output.Write("\\f");
							break;
						}
						case '\t': {
							output.Write("\\t");
							break;
						}
						default: {
							output.Write("\\u");
							output.Write(((int)c).ToString("X4"));
							break;
						}
					}
				}
			}

			if (anyEscaped) {
				if (cleanSegmentStart != str.Length) {
					output.Write(str[cleanSegmentStart..]);
				}
			}
			else {
				output.Write(str);
			}

			output.Write('\"');
			return output.ToString();
		}
	}
}