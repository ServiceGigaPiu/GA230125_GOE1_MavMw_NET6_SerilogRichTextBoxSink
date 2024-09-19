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

using System;
using System.Windows.Forms;

namespace Serilog.Sinks.RichTextBoxForms.Formatting {
	public readonly struct ValueFormatterState {
		public ValueFormatterState(RichTextBox richTextBox, int indentLvl, bool useSpacesInsteadOfTabs, int singleIndentSpaceCount) : this(
			 richTextBox: richTextBox,
			 format: string.Empty,
			 isTopLevel: false,
			 indentLvl: indentLvl,
			 useSpacesInsteadOfTabs: useSpacesInsteadOfTabs,
			 singleIndentSpaceCount: singleIndentSpaceCount
			) {
		}

		public ValueFormatterState(RichTextBox richTextBox, string format, bool isTopLevel)
									: this(richTextBox, format, isTopLevel, 0, true, 3) {
		}

		public ValueFormatterState(RichTextBox richTextBox, string format, bool isTopLevel, int indentLvl, bool useSpacesInsteadOfTabs, int singleIndentSpaceCount) {
			RichTextBox = richTextBox;
			Format = format;
			IsTopLevel = isTopLevel;
			//edited
			if (indentLvl < 0)
				throw new ArgumentOutOfRangeException(nameof(IndentLvl));
			if (singleIndentSpaceCount < 0)
				throw new ArgumentOutOfRangeException(nameof(SingleIndentSpaceCount));
			this.IndentLvl = indentLvl;
			this.UseSpacesInsteadOfTabs = useSpacesInsteadOfTabs;
			this.SingleIndentSpaceCount = singleIndentSpaceCount;
		}

		public bool UseSpacesInsteadOfTabs { get; }
		public int SingleIndentSpaceCount { get; }
		public int IndentLvl { get; }

		public string Format { get; }
		public bool IsTopLevel { get; }
		public RichTextBox RichTextBox { get; }

		public ValueFormatterState Next() {
			return new ValueFormatterState(RichTextBox, this.IndentLvl, this.UseSpacesInsteadOfTabs, this.SingleIndentSpaceCount);
		}

		public ValueFormatterState ToIndentUp() {
			return new ValueFormatterState(RichTextBox, Format, this.IsTopLevel, this.IndentLvl + 1, this.UseSpacesInsteadOfTabs, this.SingleIndentSpaceCount);
		}
		public ValueFormatterState ToIndentDown() {
			return new ValueFormatterState(RichTextBox, Format, this.IsTopLevel, this.IndentLvl - 1, this.UseSpacesInsteadOfTabs, this.SingleIndentSpaceCount);
		}
	}
}