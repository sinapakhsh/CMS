using System.Linq.Expressions;

namespace CMS.Application.Common.Models;

// Predicates compose without exposing an IQueryable backed by persistence.
public sealed record Specification<T>(Expression<Func<T, bool>> Criteria);
