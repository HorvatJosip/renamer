using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Renamer
{
    [DataContract]
    public class Skip
    {
        [DataMember(Name = "extensions")]
        public string[] Extensions { get; set; }

        [DataMember(Name = "fileNames")]
        public string[] Files { get; set; }

        [DataMember(Name = "directoryNames")]
        public string[] Directories { get; set; }

        /// <summary>
        /// <para>Passes in a string that is checked against a current string element in the collection.</para>
        /// <para>Explanation for the <see cref="File(string)"/> method: used to check if a file should
        /// be skipped based on <see cref="Files"/> or <see cref="Extensions"/>.</para>
        /// <para>Default implementation: (original, current) => original.Contains(current);</para>
        /// <para>In the default implementation, original can be file name or extension of the full file name
        /// that was passed in, current is current file from <see cref="Files"/> or current
        /// extension from <see cref="Extensions"/>.</para>
        /// </summary>
        public static Func<string, string, bool> StringChecker { get; set; } = (original, current) => original.Contains(current);

        public bool Directory(string directoryName)
            => Directories.HasElements() && Directories.Any(dir => StringChecker(directoryName, dir));

        public bool File(string fileName)
        {
            string extension = null;
            var dotIndex = fileName.LastIndexOf('.');
            
            if(dotIndex != -1)
            {
                extension = fileName.Substring(dotIndex + 1, fileName.Length - dotIndex - 1);
                fileName = fileName.Substring(0, dotIndex);
            }

            if (extension != null && Extensions.HasElements() && Extensions.Any(ext => StringChecker(extension, ext)))
                return true;

            if (Files.HasElements() && Files.Any(file => StringChecker(fileName, file)))
                return true;

            return false;
        }

        public override string ToString() => string.Format("\tExtensions: {0}{1}\tFiles: {2}{3}\tDirectories: {4}",
            string.Join(", ", Extensions ?? new string[0]),
            Environment.NewLine,
            string.Join(", ", Files ?? new string[0]),
            Environment.NewLine,
            string.Join(", ", Directories ?? new string[0])
        );
    }
}
