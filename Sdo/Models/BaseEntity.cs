// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// BaseEntity.cs
//
// Base class for all entity models in the SDO application.

using System;

namespace Sdo.Models
{
    /// <summary>
    /// Base class for all entities in the SDO system.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last modification timestamp.
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Validates the entity data.
        /// </summary>
        /// <returns>True if the entity is valid, false otherwise.</returns>
        public virtual bool IsValid()
        {
            return !string.IsNullOrEmpty(Id);
        }
    }
}