using System;
using System.Collections.Generic;

namespace KeepSimple.Configuration
{
    public class Configuration : IConfiguration
    {
        #region Fields

        private readonly string _name;
        private readonly IConfiguration _parent;
        private readonly Dictionary<string, IConfiguration> _children;
        private readonly Dictionary<string, string> _values;

        #endregion

        #region Constructor

        public Configuration()
            : this(null, null)
        {
        }

        public Configuration(string name, IConfiguration parent)
        {
            _name = name;
            _parent = parent;
            _children = new Dictionary<string, IConfiguration>();
            _values = new Dictionary<string, string>();
        }

        #endregion

        #region Methods

        public void AddValue(string path, string value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Configuration current = this;

            var parts = ConfigurationPath.Split(path);
            if (parts.Length > 1)
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (!current._children.ContainsKey(parts[i]))
                    {
                        var newChild = new Configuration(parts[i], current);
                        current._children.Add(parts[i], newChild);
                    }

                    current = (Configuration)current._children[parts[i]];
                }
            }

            current._values[parts[parts.Length - 1]] = value;
        }

        public string GetValue(string path)
        {
            var childPath = ConfigurationPath.SkipLast(path);
            var valueName = ConfigurationPath.GetLast(path);

            IConfiguration config = this;
            if (childPath != null)
                config = GetChildren(childPath);

            if (config != null)
            {
                if (config.Values.TryGetValue(valueName, out string result))
                    return result;
            }

            return null;
        }

        public bool RemoveValue(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var childPath = ConfigurationPath.SkipLast(path);
            var valueName = ConfigurationPath.GetLast(path);

            Configuration children = this;

            if (childPath != null)
                children = (Configuration)GetChildren(childPath);

            if (children != null)
            {
                children._values.Remove(valueName);
                return true;
            }

            return false;
        }

        public IConfiguration GetChildren(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            IConfiguration result = null;

            var parts = ConfigurationPath.Split(path);
            foreach (var part in parts)
            {
                if (result == null)
                {
                    if (!Children.TryGetValue(part, out result))
                        break;
                }
                else
                {
                    if (!result.Children.TryGetValue(part, out result))
                        break;
                }
            }

            return result;
        }

        public bool RemoveChildren(string path)
        {
            var childToRemove = GetChildren(path);
            if (childToRemove != null)
            {
                var childName = ConfigurationPath.GetLast(path);
                return ((Configuration)childToRemove.Parent)._children.Remove(childName);
            }

            return false;
        }

        public override string ToString()
        {
            if (_name != null)
                return $"{Path} Children:{_children.Count} Values:{_values.Count}";
            else
                return $"Children:{_children.Count} Values:{_values.Count}";
        }

        #endregion

        #region Properties

        public IConfiguration Parent
        {
            get { return _parent; }
        }

        public string Path
        {
            get
            {
                if (_parent != null)
                {
                    if (_parent.Name != null)
                        return _parent.Name + ConfigurationPath.PathSeparator + _name;
                    else
                        return _name;
                }
                else
                {
                    return _name;
                }
            }
        }

        public string Name
        {
            get { return _name; }
        }

        public IReadOnlyDictionary<string, IConfiguration> Children
        {
            get { return _children; }
        }

        public IReadOnlyDictionary<string, string> Values
        {
            get { return _values; }
        }

        #endregion
    }
}