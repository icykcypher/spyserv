﻿namespace UserService.Model
{
    public class RoleEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public virtual ICollection<PermissionEntity> Permissions { get; set; } = [];

        public virtual ICollection<User> Users { get; set; } = [];
    }
}