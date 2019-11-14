using Monaco.Options;
using System;

namespace Monaco
{
	public class EditorModel
	{
        public EditorModel() { }

        public EditorModel(EditorOptions options)
        {
            Options = options;
        }

        public string Id { get; set; } = $"Blazaco_{new Random().Next(0, 1000000).ToString()}";
        public EditorOptions Options { get; set; } = new EditorOptions();
    }
}
