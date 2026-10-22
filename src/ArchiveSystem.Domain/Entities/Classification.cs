// src/ArchiveSystem.Domain/Entities/Classification.cs
using System.Text.Json;

namespace ArchiveSystem.Domain.Entities
{
    public class Classification : BaseEntity<long>
    {
        public string Title { get; private set; }
        public long? ParentId { get; private set; }
        public string Path { get; private set; } // Materialized Path: /1/5/10/
        public int Level { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt.HasValue;
        public string? SystemKey { get; private set; }
        public bool IsSystem => !string.IsNullOrWhiteSpace(SystemKey);
        public string Description { get; private set; }
        public string ColorCode { get; private set; }
        public string Icon { get; private set; }
        
        // Dynamic Fields Schema (JSON)
        public string DynamicFieldGroups { get; private set; } = "{}"; // JSON schema for dynamic fields

        // Navigation properties
        public Classification? Parent { get; private set; }
        public ICollection<Classification> Children { get; private set; } = new List<Classification>();
        public ICollection<Case> Cases { get; private set; } = new List<Case>();
        public ICollection<Dossier> Dossiers { get; private set; } = new List<Dossier>();
        public ICollection<DynamicTemplate> DynamicTemplates { get; private set; } = new List<DynamicTemplate>();

        // Private parameterless constructor for ORMs
        private Classification() : base()
        {
            Title = string.Empty;
            Path = "/";
            Description = string.Empty;
            ColorCode = "#000000";
            Icon = "folder";
        }

        public Classification(long id, string title, long? parentId = null, string? description = null, string? colorCode = null, string? icon = null)
            : base(id)
        {
            ValidateTitle(title);

            Title = title;
            ParentId = parentId;
            Path = parentId.HasValue ? $"/{parentId}/" : "/";
            Level = parentId.HasValue ? 1 : 0;
            IsActive = true;
            Description = description ?? string.Empty;
            ColorCode = colorCode ?? "#000000";
            Icon = icon ?? "folder";
        }

        public void UpdateTitle(string title)
        {
            EnsureNotSystem("System classifications cannot be renamed.");
            ValidateTitle(title);
            Title = title;
            MarkAsModified();
        }

        public void UpdateDescription(string description)
        {
            Description = description;
            MarkAsModified();
        }

        public void UpdateAppearance(string? colorCode, string? icon)
        {
            ColorCode = string.IsNullOrWhiteSpace(colorCode) ? ColorCode : colorCode;
            Icon = string.IsNullOrWhiteSpace(icon) ? Icon : icon;
            MarkAsModified();
        }

        public void Activate()
        {
            IsActive = true;
            DeletedAt = null;
            MarkAsModified();
        }

        public void Deactivate()
        {
            IsActive = false;
            MarkAsModified();
        }

        public void SoftDelete()
        {
            EnsureNotSystem("System classifications cannot be deleted.");
            if (IsDeleted)
            {
                return;
            }

            IsActive = false;
            DeletedAt = DateTime.UtcNow;
            MarkAsModified();
        }

        public void Restore()
        {
            if (!IsDeleted)
            {
                return;
            }

            IsActive = true;
            DeletedAt = null;
            MarkAsModified();
        }

        public void SetParent(Classification parent)
        {
            EnsureNotSystem("System classifications cannot be moved.");
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (parent.Id == Id)
                throw new ArgumentException("A classification cannot be its own parent.", nameof(parent));

            // Prevent circular references
            if (parent.Path.Contains($"/{Id}/"))
                throw new ArgumentException("Cannot set parent to a descendant.", nameof(parent));

            ParentId = parent.Id;
            Parent = parent;
            Level = parent.Level + 1;
            Path = $"{parent.Path}{Id}/";
            MarkAsModified();
        }

        public void RemoveFromParent()
        {
            EnsureNotSystem("System classifications cannot be moved.");
            ParentId = null;
            Parent = null;
            Level = 0;
            Path = $"/{Id}/";
            MarkAsModified();
        }

        public void RebaseHierarchy(string path, int level)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be empty.", nameof(path));
            }

            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Level cannot be negative.");
            }

            Path = path;
            Level = level;
            MarkAsModified();
        }

        public bool IsAncestorOf(Classification classification)
        {
            return classification.Path.StartsWith(Path) && classification.Id != Id;
        }

        public bool IsDescendantOf(Classification classification)
        {
            return Path.StartsWith(classification.Path) && Id != classification.Id;
        }

        public string GetFullPath()
        {
            return Path.TrimEnd('/');
        }

        public void SetDynamicFieldGroups(JsonDocument dynamicFieldGroups)
        {
            DynamicFieldGroups = dynamicFieldGroups.RootElement.GetRawText();
            MarkAsModified();
        }

        public void MarkAsSystem(string systemKey)
        {
            if (string.IsNullOrWhiteSpace(systemKey))
            {
                throw new ArgumentException("System key cannot be empty.", nameof(systemKey));
            }

            SystemKey = systemKey.Trim();
            IsActive = true;
            DeletedAt = null;
            MarkAsModified();
        }

        private void EnsureNotSystem(string message)
        {
            if (IsSystem)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Classification title cannot be null or empty.", nameof(title));

            if (title.Length > 200)
                throw new ArgumentException("Classification title cannot exceed 200 characters.", nameof(title));
        }
    }
}
