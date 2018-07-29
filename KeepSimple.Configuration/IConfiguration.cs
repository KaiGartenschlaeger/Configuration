using System.Collections.Generic;

namespace KeepSimple.Configuration
{
    /// <summary>
    /// Stellt Anwendungseinstellungen zur Verfügung.
    /// </summary>
    public interface IConfiguration
    {
        #region Methods

        /// <summary>
        /// Fügt einen neuen Wert hinzu.
        /// </summary>
        /// <param name="path">Pfad des Wertes, der hinzugefügt werden soll.</param>
        /// <param name="value">Wert der hinzugefügt werden soll.</param>
        void AddValue(string path, string value);

        /// <summary>
        /// Entfernt einen Wert.
        /// </summary>
        /// <param name="path">Pfad des Wertes, der entfernt werden soll.</param>
        bool RemoveValue(string path);

        /// <summary>
        /// Liefert einen Wert.
        /// </summary>
        /// <param name="path">Pfad des Wertes, der zurückgegeben werden soll.</param>
        /// <returns>Wert der ermittelt wurde. Andernfalls null.</returns>
        string GetValue(string path);

        /// <summary>
        /// Liefert ein Unterbereich.
        /// </summary>
        /// <param name="path">Pfad des Unterbereichs, der zurückgegeben werden soll.</param>
        /// <returns>Unterbereich oder null, falls nicht vorhanden.</returns>
        IConfiguration GetChildren(string path);

        /// <summary>
        /// Entfernt einen Unterbereich.
        /// </summary>
        /// <param name="path">Pfad des Unterbereichs, der entfernt werden soll.</param>
        bool RemoveChildren(string path);

        #endregion

        #region Values

        /// <summary>
        /// Enthält den übergeordneten Bereich.
        /// </summary>
        IConfiguration Parent { get; }

        /// <summary>
        /// Enthält den absoluten Pfad innerhalb der Konfiguration.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Name des aktuellen Bereichs.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Enthält Unterbereiche.
        /// </summary>
        IReadOnlyDictionary<string, IConfiguration> Children { get; }

        /// <summary>
        /// Enthält die Werte des aktuellen Bereichs.
        /// </summary>
        IReadOnlyDictionary<string, string> Values { get; }

        #endregion
    }
}