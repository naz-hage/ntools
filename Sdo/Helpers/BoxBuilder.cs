using System;
using System.Collections.Generic;
using System.Text;

namespace rb_anlyze
{
    public class BoxBuilder
    {
        private readonly List<string[]> _rows = new();
        private readonly string[] _headers;

        public BoxBuilder(params string[] headers)
        {
            _headers = headers;
        }

        public void AddRow(params string[] columns)
        {
            _rows.Add(columns);
        }

        public string Build()
        {
            int cols = _headers.Length;

            // Compute max width for each column
            int[] widths = new int[cols];
            for (int i = 0; i < cols; i++)
            {
                widths[i] = _headers[i].Length;
            }

            foreach (var row in _rows)
            {
                for (int i = 0; i < cols; i++)
                {
                    widths[i] = Math.Max(widths[i], row[i].Length);
                }
            }

            // Helper to pad text
            string Pad(string text, int width) =>
                text + new string(' ', width - text.Length);

            // Build top border
            var sb = new StringBuilder();
            sb.Append('┌');
            for (int i = 0; i < cols; i++)
            {
                sb.Append(new string('─', widths[i]));
                sb.Append(i == cols - 1 ? '┐' : '┬');
            }
            sb.AppendLine();

            // Header row
            sb.Append('│');
            for (int i = 0; i < cols; i++)
            {
                sb.Append(Pad(_headers[i], widths[i]));
                sb.Append('│');
            }
            sb.AppendLine();

            // Header separator
            sb.Append('├');
            for (int i = 0; i < cols; i++)
            {
                sb.Append(new string('─', widths[i]));
                sb.Append(i == cols - 1 ? '┤' : '┼');
            }
            sb.AppendLine();

            // Data rows
            foreach (var row in _rows)
            {
                sb.Append('│');
                for (int i = 0; i < cols; i++)
                {
                    sb.Append(Pad(row[i], widths[i]));
                    sb.Append('│');
                }
                sb.AppendLine();
            }

            // Bottom border
            sb.Append('└');
            for (int i = 0; i < cols; i++)
            {
                sb.Append(new string('─', widths[i]));
                sb.Append(i == cols - 1 ? '┘' : '┴');
            }

            return sb.ToString();
        }
    }

}
