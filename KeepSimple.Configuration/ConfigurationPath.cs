using System;
using System.Collections.Generic;
using System.Text;

namespace KeepSimple.Configuration
{
    /// <summary>
    /// Enthält Hilfsmethoden zur Umwandlung des Konfigurationspfades.
    /// </summary>
    public static class ConfigurationPath
    {
        #region Constants

        /// <summary>
        /// Das Trennzeichen für Pfadabschnitte.
        /// </summary>
        public const char PathSeparator = ':';

        #endregion

        #region Methods

        /// <summary>
        /// Kombiniert einen Pfad aus einzelnen Abschnitten.
        /// </summary>
        /// <param name="parts">Abschnitte die zusammengefügt werden sollen.</param>
        /// <returns>Der Vollständige Pfad</returns>
        public static string Combine(IEnumerable<string> parts)
        {
            if (parts == null)
                throw new ArgumentNullException(nameof(parts));

            var buffer = new StringBuilder();

            foreach (var part in parts)
            {
                buffer.Append(part);
                buffer.Append(PathSeparator);
            }

            if (buffer.Length > 0)
                buffer.Length -= 1;

            return buffer.ToString();
        }

        /// <summary>
        /// Teilt einen Pfad in die einzelnen Abschnitte auf.
        /// </summary>
        /// <param name="path">Pfad der aufgeteilt werden soll.</param>
        /// <returns>Abschnitte des Pfades.</returns>
        public static string[] Split(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return path.Split(PathSeparator);
        }

        /// <summary>
        /// Entfernt den letzten Abschnitt vom Pfad.
        /// </summary>
        /// <param name="path">Pfad bei dem der letzte Abschnitt entfernt werden soll.</param>
        /// <returns>Der gekürzte Pfad. Falls der Pfad nur einen Abschnitt enthält, wird null zurückgegeben.</returns>
        public static string SkipLast(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parts = Split(path);
            if (parts.Length == 1)
                return null;
            else
                return string.Join(PathSeparator.ToString(), parts, 0, parts.Length - 1);
        }

        /// <summary>
        /// Liefert den letzten Abschnitt vom Pfad.
        /// </summary>
        /// <param name="path">Der zu verwendende Pfad.</param>
        /// <returns>Der letzte Abschnitt im Pfad.</returns>
        public static string GetLast(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parts = Split(path);
            return parts[parts.Length - 1];
        }

        #endregion
    }
}