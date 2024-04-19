namespace Tool.Compet.EntityFrameworkCore;

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension for IQueryable<T>, where T is item type.
/// </summary>
public static class PaginationExt {
	/// <summary>
	/// Note: given `page * item` must be in range of Int32.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="query"></param>
	/// <param name="page">Page number (1-based index, must > 0)</param>
	/// <param name="limit">Item count (must >= 0) in the page. For eg,. 10, 20, 50,...</param>
	/// <param name="startPaddingItems">Items which be added at left of query result.</param>
	/// <param name="endPaddingItems">Items which be added at right of query result.</param>
	/// <returns></returns>
	public static async Task<PagedResult<T>> PaginateAsyncDk<T>(
		this IQueryable<T> query,
		int page,
		int limit,
		IEnumerable<T>? startPaddingItems = null,
		IEnumerable<T>? endPaddingItems = null
	) where T : class {
		if (page <= 0 || limit < 0) {
			throw new InvalidDataException("Required: page > 0 and limit >= 0");
		}

		// Offset equals to item-count so far.
		var offset = Math.Max(0, (page - 1) * limit);

		var startItemCount = startPaddingItems?.Count() ?? 0;
		var endItemCount = endPaddingItems?.Count() ?? 0;
		var queryCount = await query.CountAsync();
		var totalItemCount = startItemCount + queryCount + endItemCount;

		// Take items in range [offset, offset + limit) on total items
		var takeItems = new List<T>();
		int skipCount;
		int takeCount;
		// Start
		if (startPaddingItems != null) {
			takeCount = Math.Min(startItemCount - offset, limit);
			if (takeCount > 0) {
				skipCount = offset;
				takeItems.AddRange(startPaddingItems.Skip(skipCount).Take(takeCount));
			}
		}
		// Query
		takeCount = limit - takeItems.Count;
		if (takeCount > 0) {
			skipCount = Math.Max(0, offset - startItemCount);
			takeItems.AddRange(await query.Skip(skipCount).Take(takeCount).ToArrayAsync());
		}
		// End
		if (endPaddingItems != null) {
			takeCount = limit - takeItems.Count;
			if (takeCount > 0) {
				skipCount = Math.Max(0, offset - startItemCount - queryCount);
				takeItems.AddRange(endPaddingItems.Skip(skipCount).Take(takeCount));
			}
		}

		// This calculation is faster than `Math.Ceiling(totalItemCount / limit)`
		var pageCount = Math.Max(0, totalItemCount + limit - 1) / limit;

		return new PagedResult<T>(
			items: [.. takeItems],
			pager: new(page, pageCount, totalItemCount)
		);
	}
}

public class PagedResult<T>(T[] items, Pager pager) where T : class {
	/// Items in the page.
	/// Note: can use `IEnumerable<T>` for more abstract that can cover both of array and list.
	public readonly T[] items = items;

	public readonly Pager pager = pager;
}

public class Pager(int page, int count, int total) {
	/// Page position (1-based index)
	[JsonPropertyName("page")]
	public int page { get; set; } = page;

	/// Page count
	[JsonPropertyName("count")]
	public int count { get; set; } = count;

	/// Total item count
	[JsonPropertyName("total")]
	public int total { get; set; } = total;
}
