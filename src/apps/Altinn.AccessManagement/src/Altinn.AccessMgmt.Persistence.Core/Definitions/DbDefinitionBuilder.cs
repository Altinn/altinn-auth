using System.Linq.Expressions;
using System.Reflection;

namespace Altinn.AccessMgmt.Persistence.Core.Definitions
{
    /// <summary>
    /// Provides a fluent API for building a <see cref="Definitions.DbDefinition"/> for the entity type <typeparamref name="T"/>.
    /// This builder allows configuration of basic, extended, and cross-reference properties for the database definition.
    /// </summary>
    /// <typeparam name="T">The entity type for which the definition is being built.</typeparam>
    public sealed class DbDefinitionBuilder<T>
    {
        /// <summary>
        /// The underlying database definition that is being constructed.
        /// </summary>
        private DbDefinition DbDefinition { get; set; } = new(typeof(T));

        /// <summary>
        /// Returns the built <see cref="Definitions.DbDefinition"/> after configuration.
        /// </summary>
        /// <returns>The configured <see cref="Definitions.DbDefinition"/>.</returns>
        public DbDefinition Build()
        {
            return DbDefinition;
        }

        #region Basic

        /// <summary>
        /// Sets whether the entity is a view.
        /// </summary>
        /// <param name="value">Default: true</param>
        /// <param name="version">View version to trigger recreate (default: 1)</param>
        /// <returns></returns>
        public DbDefinitionBuilder<T> IsView(bool value = true, int version = 1)
        {
            // Add view script??
            DbDefinition.IsView = value;
            DbDefinition.ViewVersion = version;
            return this;
        }

        /// <summary>
        /// Sets whether the entity is a view.
        /// </summary>
        /// <param name="query">Sql Query</param>
        /// <returns></returns>
        public DbDefinitionBuilder<T> SetViewQuery(string query)
        {
            DbDefinition.ViewQuery = query;
            return this;
        }

        /// <summary>
        /// Sets whether the entity is a view.
        /// </summary>
        /// <returns></returns>
        public DbDefinitionBuilder<T> AddViewDependency<TDep>()
        {
            DbDefinition.ViewDependencies.Add(typeof(TDep));
            return this;
        }

        /// <summary>
        /// Sets whether the entity supports translation.
        /// </summary>
        /// <param name="value">If set to <c>true</c>, translations are enabled; otherwise, they are disabled.</param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        public DbDefinitionBuilder<T> EnableTranslation(bool value = true)
        {
            DbDefinition.HasTranslation = value;
            return this;
        }

        /// <summary>
        /// Sets whether the entity supports history tracking.
        /// </summary>
        /// <param name="value">If set to <c>true</c>, history is enabled; otherwise, it is disabled.</param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        public DbDefinitionBuilder<T> EnableHistory(bool value = true)
        {
            DbDefinition.HasHistory = value;
            return this;
        }

        /// <summary>
        /// Registers a property as a column in the database definition.
        /// </summary>
        /// <param name="column">An expression that identifies the property to register.</param>
        /// <param name="nullable">Indicates whether the column can contain null values.</param>
        /// <param name="defaultValue">The default value for the column, if any.</param>
        /// <param name="length">The maximum length of the column (if applicable).</param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        public DbDefinitionBuilder<T> RegisterProperty(Expression<Func<T, object>> column, bool nullable = false, string defaultValue = null, int? length = null)
        {
            var propertyInfo = ExtractPropertyInfo(column);
            var propertyType = propertyInfo.PropertyType;

            // Hvis typen er en kompleks type, send den til `RegisterComplexProperty`
            if (propertyType.Namespace != null && propertyType.Namespace == typeof(T).Namespace
                && !propertyType.IsPrimitive && propertyType != typeof(string))
            {
                RegisterComplexProperty(propertyType, propertyInfo.Name, nullable);
            }
            else
            {
                // Registrer normal (ikke-kompleks) egenskap
                var columnDef = new DbPropertyDefinition()
                {
                    Name = propertyInfo.Name,
                    Property = propertyInfo,
                    DefaultValue = defaultValue,
                    IsNullable = nullable
                };

                DbDefinition.Properties.Add(columnDef);
            }

            return this;
        }

        private void RegisterComplexProperty(Type complexType, string parentPrefix, bool nullable)
        {
            foreach (var subProperty in complexType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string prefix = $"{parentPrefix}_{subProperty.Name}";

                if (subProperty.PropertyType.Namespace != null &&
                    subProperty.PropertyType.Namespace == typeof(T).Namespace &&
                    !subProperty.PropertyType.IsPrimitive &&
                    subProperty.PropertyType != typeof(string))
                {
                    // Rekursivt kall for nested objekter
                    RegisterComplexProperty(subProperty.PropertyType, prefix, nullable);
                }
                else
                {
                    // Registrer primitiv egenskap med korrekt prefiks
                    var columnDef = new DbPropertyDefinition()
                    {
                        Name = prefix,
                        Property = subProperty,
                        IsNullable = nullable
                    };

                    DbDefinition.Properties.Add(columnDef);
                }
            }
        }

        /// <summary>
        /// Registers a constraint for the specified properties, either as a primary key or a unique constraint.
        /// </summary>
        /// <param name="properties">A collection of expressions identifying the properties.</param>
        /// <param name="isPrimaryKey">Specifies whether this is a primary key constraint.</param>
        /// <param name="includedProperties">A collection of expressions identifying the properties to be included in an unique covering index</param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        /// <exception cref="Exception">Thrown if any of the specified properties does not exist on the model type.</exception>
        private DbDefinitionBuilder<T> RegisterConstraint(IEnumerable<Expression<Func<T, object>>> properties, bool isPrimaryKey, IEnumerable<Expression<Func<T, object>>> includedProperties)
        {
            var propertyDefinitions = new Dictionary<string, Type>();
            var includedPropertyDefinitions = new Dictionary<string, Type>();
            var propertyInfos = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p.PropertyType);

            foreach (var property in properties)
            {
                var propertyInfo = ExtractPropertyInfo(property);

                if (!propertyInfos.ContainsKey(propertyInfo.Name))
                {
                    throw new Exception($"{typeof(T).Name} does not contain the property '{propertyInfo.Name}'");
                }

                propertyDefinitions[propertyInfo.Name] = propertyInfo.PropertyType;
            }

            if (includedProperties != null)
            {
                foreach (var property in includedProperties)
                {
                    var propertyInfo = ExtractPropertyInfo(property);

                    if (!propertyInfos.ContainsKey(propertyInfo.Name))
                    {
                        throw new Exception($"{typeof(T).Name} does not contain the property '{propertyInfo.Name}'");
                    }

                    includedPropertyDefinitions[propertyInfo.Name] = propertyInfo.PropertyType;
                }
            }

            var constraint = new DbConstraintDefinition()
            {
                Properties = propertyDefinitions,
                IsPrimaryKey = isPrimaryKey,
                IncludedProperties = includedPropertyDefinitions
            };

            if (isPrimaryKey)
            {
                constraint.Name = $"PK_{typeof(T).Name}";
                DbDefinition.Constraints.Add(constraint);
            }
            else
            {
                constraint.Name = $"UC_{typeof(T).Name}_{string.Join("_", propertyDefinitions.Keys)}";
                DbDefinition.Constraints.Add(constraint);
            }

            return this;
        }

        /// <summary>
        /// Registers one or more properties as the primary key for the model.
        /// </summary>
        /// <param name="properties">A collection of expressions identifying the properties that form the primary key.</param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        public DbDefinitionBuilder<T> RegisterPrimaryKey(IEnumerable<Expression<Func<T, object>>> properties)
            => RegisterConstraint(properties, isPrimaryKey: true, includedProperties: null);

        /// <summary>
        /// Registers a unique constraint for the specified properties.
        /// </summary>
        /// <param name="properties">A collection of expressions identifying the properties that should have a unique constraint.</param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        public DbDefinitionBuilder<T> RegisterUniqueConstraint(IEnumerable<Expression<Func<T, object>>> properties, IEnumerable<Expression<Func<T, object>>> includedProperties = null)
            => RegisterConstraint(properties, isPrimaryKey: false, includedProperties: includedProperties);

        #endregion

        #region Extended

        /// <summary>
        /// Registers an extended property that defines a relationship between the primary entity and an extended entity.
        /// </summary>
        /// <typeparam name="TExtended">The type of the extended entity.</typeparam>
        /// <typeparam name="TJoin">The type used for joining the entities.</typeparam>
        /// <param name="TProperty">An expression selecting the property on the primary entity.</param>
        /// <param name="TJoinProperty">An expression selecting the property on the join entity.</param>
        /// <param name="TExtendedProperty">An expression selecting the property on the extended entity.</param>
        /// <param name="optional">Indicates whether the relationship is optional.</param>
        /// <param name="isList">Indicates whether the relationship represents a collection.</param>
        /// <param name="cascadeDelete">Indicates whether cascade delete should be used for the relationship.</param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        public DbDefinitionBuilder<T> RegisterExtendedProperty<TExtended, TJoin>(
            Expression<Func<T, object>> TProperty,
            Expression<Func<TJoin, object>> TJoinProperty,
            Expression<Func<TExtended, TJoin>> TExtendedProperty,
            bool optional = false,
            bool isList = false,
            bool cascadeDelete = false)
        {
            string baseProperty = ExtractPropertyInfo(TProperty).Name;
            string refProperty = ExtractPropertyInfo(TJoinProperty).Name;
            string extendedProperty = ExtractPropertyInfo(TExtendedProperty).Name;
            string extendedPropertyType = ExtractPropertyInfo(TExtendedProperty).PropertyType.Name;

            if (extendedPropertyType != typeof(TJoin).Name)
            {
                Console.WriteLine($"WARNING: Type missmatch on definition for '{typeof(T).Name}'");
            }

            var join = new DbRelationDefinition()
            {
                Name = $"FK_{typeof(T).Name}_{extendedProperty}_{typeof(TJoin).Name}",
                Base = typeof(T),
                Ref = typeof(TJoin),
                ExtendedType = typeof(TExtended),
                BaseProperty = baseProperty,
                RefProperty = refProperty,
                ExtendedProperty = extendedProperty,
                IsOptional = optional,
                IsList = isList,
                UseCascadeDelete = cascadeDelete
            };
            DbDefinition.Relations.Add(join);

            return this;
        }
        #endregion

        #region Cross

        /// <summary>
        /// Registers cross-reference relationships with extended properties for both related entity sets.
        /// This method internally registers extended properties for both sides and then defines the cross-reference.
        /// </summary>
        /// <typeparam name="TExtended">The type of the extended entity.</typeparam>
        /// <typeparam name="TA">The type of the first related entity.</typeparam>
        /// <typeparam name="TB">The type of the second related entity.</typeparam>
        /// <param name="defineA">
        /// A tuple containing expressions for the primary source property, join property, and extended property for the first related entity.
        /// </param>
        /// <param name="defineB">
        /// A tuple containing expressions for the primary source property, join property, and extended property for the second related entity.
        /// </param>
        /// <returns>The current <see cref="DbDefinitionBuilder{T}"/> instance for fluent chaining.</returns>
        public DbDefinitionBuilder<T> RegisterAsCrossReferenceExtended<TExtended, TA, TB>(
            (Expression<Func<T, object>> Source, Expression<Func<TA, object>> Join, Expression<Func<TExtended, TA>> Extended, bool? CascadeDelete) defineA,
            (Expression<Func<T, object>> Source, Expression<Func<TB, object>> Join, Expression<Func<TExtended, TB>> Extended, bool? CascadeDelete) defineB
        )
        {
            RegisterExtendedProperty(defineA.Source, defineA.Join, defineA.Extended, defineA.CascadeDelete ?? false);
            RegisterExtendedProperty(defineB.Source, defineB.Join, defineB.Extended, defineB.CascadeDelete ?? false);

            var crossRef = new DbCrossRelationDefinition(
               crossType: typeof(T),
               crossExtendedType: typeof(TExtended),
               AType: typeof(TA),
               AIdentityProperty: ExtractPropertyInfo(defineA.Source).Name,
               AReferenceProperty: ExtractPropertyInfo(defineA.Join).Name,
               BType: typeof(TB),
               BIdentityProperty: ExtractPropertyInfo(defineB.Source).Name,
               BReferenceProperty: ExtractPropertyInfo(defineB.Join).Name
           );

            DbDefinition.CrossRelation = crossRef;

            return this;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Extracts the <see cref="PropertyInfo"/> from the given property expression.
        /// </summary>
        /// <typeparam name="TLocal">The type of the object containing the property.</typeparam>
        /// <param name="expression">An expression that selects a property of <typeparamref name="TLocal"/>.</param>
        /// <returns>The <see cref="PropertyInfo"/> corresponding to the property in the expression.</returns>
        /// <exception cref="ArgumentException">Thrown if the expression does not refer to a valid property.</exception>
        private PropertyInfo ExtractPropertyInfo<TLocal>(Expression<Func<TLocal, object>> expression)
        {
            MemberExpression memberExpression;

            if (expression.Body is MemberExpression)
            {
                memberExpression = (MemberExpression)expression.Body;
            }
            else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            {
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                throw new ArgumentException("Expression must refer to a property.");
            }

            return memberExpression.Member as PropertyInfo ?? throw new ArgumentException("Member is not a property.");
        }

        private PropertyInfo ExtractPropertyInfo<TLocal, TLocalJoin>(Expression<Func<TLocal, TLocalJoin>> expression)
        {
            MemberExpression memberExpression;

            if (expression.Body is MemberExpression)
            {
                memberExpression = (MemberExpression)expression.Body;
            }
            else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            {
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                throw new ArgumentException("Expression must refer to a property.");
            }

            return memberExpression.Member as PropertyInfo ?? throw new ArgumentException("Member is not a property.");
        }

        #endregion
    }
}
