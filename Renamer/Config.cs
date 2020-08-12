using System.Runtime.Serialization;

namespace Renamer
{
    [DataContract]
    public class Config
    {
        [DataMember(Name = "skip")]
        public Skip Skip { get; set; }

        [DataMember(Name = "matchCase")]
        public bool MatchCase { get; set; }

        [DataMember(Name = "fullWord")]
        public bool FullWord { get; set; }

        [DataMember(Name = "fullWordRegex")]
        public string FullWordRegex { get; set; }

        [DataMember(Name = "renameFiles")]
        public bool RenameFiles { get; set; }

        [DataMember(Name = "renameDirectories")]
        public bool RenameDirectories { get; set; }

        [DataMember(Name = "renameWithinFileContent")]
        public bool RenameWithinFileContent { get; set; }

        [DataMember(Name = "useFileExtensionWhileRenamingFile")]
        public bool UseFileExtensionWhileRenamingFile { get; set; }

        [DataMember(Name = "openDirectoryAfter")]
        public bool OpenDirectoryAfter { get; set; }

        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            foreach (var prop in GetType().GetProperties())
            {
                builder.Append(prop.Name);
                builder.Append(": ");
                if (prop.PropertyType.IsValueType == false && prop.PropertyType != typeof(string))
                    builder.AppendLine();

                builder.AppendLine(prop.GetValue(this)?.ToString());
            }

            return builder.ToString();
        }
    }
}
