namespace Tool.Compet.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

// Extension of IQueryable<T>
// Where T: item type
public static class PaginationExt {
	/// Note: given `pageIndex * pageSize` must be in range of Int32.
	/// @param pagePos: Index (from 1) of the page. For eg,. 1, 2, 3,...
	/// @param pageSize: Item count in the page. For eg,. 10, 20, 50,...
	public static async Task<PagedResult<T>> PaginateDk<T>(
		this IQueryable<T> query,
		int pagePos,
		int pageSize
	) where T : class {
		// Offset equals to item-count as far to now. For eg,. offset is 50 when query at page 2 with item 50.
		// TechNote: use max to prevent negative index from overflow
		var offset = Math.Max(0, (pagePos - 1) * pageSize);

		// Number of all items of the query.
		var totalItemCount = await query.CountAsync();

		// Query and take some items in range [offset, offset + pageSize - 1]
		var items = await query.Skip(offset).Take(pageSize).ToArrayAsync();

		// Number of page.
		// This calculation is faster than `Math.Ceiling(rowCount / pageSize)`
		var pageCount = (totalItemCount + pageSize - 1) / pageSize;

		return new PagedResult<T>(
			items: items,
			pagePos: pagePos,
			pageCount: pageCount,
			totalItemCount: totalItemCount
		);
	}

	/// Note: given `pageIndex * pageSize` must be in range of Int32.
	/// @param leftPaddingItems: Items which be added at left of query result.
	/// @param pagePos: Index (from 1) of the page. For eg,. 1, 2, 3,...
	/// @param pageSize: Item count in the page. For eg,. 10, 20, 50,...
	public static async Task<PagedResult<T>> PaginateDk<T>(
		this IQueryable<T> query,
		IEnumerable<T> leftPaddingItems,
		int pagePos,
		int pageSize
	) where T : class {
		// Offset equals to item-count as far to now. For eg,. offset is 50 when query at page 2 with item 50.
		// TechNote: use max to prevent negative index from overflow
		var offset = Math.Max(0, (pagePos - 1) * pageSize);

		var leftPaddingItemCount = leftPaddingItems.Count();
		var totalItemCount = leftPaddingItemCount + (await query.CountAsync());

		// Query and take some items in range [offset, offset + pageSize - 1]
		var items = new List<T>();
		var takeCount1 = Math.Min(leftPaddingItemCount - offset, pageSize);
		if (takeCount1 > 0) {
			items.AddRange(leftPaddingItems.Skip(offset).Take(takeCount1));
		}
		var takeCount2 = pageSize - items.Count;
		if (takeCount2 > 0) {
			var skipCount = Math.Max(0, offset - leftPaddingItemCount);
			items.AddRange(await query.Skip(skipCount).Take(takeCount2).ToArrayAsync());
		}

		// This calculation is faster than `Math.Ceiling(rowCount / pageSize)`
		var pageCount = (totalItemCount + pageSize - 1) / pageSize;

		return new PagedResult<T>(
			items: items.ToArray(),
			pagePos: pagePos,
			pageCount: pageCount,
			totalItemCount: totalItemCount
		);
	}
}

public class PagedResult<T> where T : class {
	/// Items in the page.
	/// Note: can use `IEnumerable<T>` for more abstract that can cover both of array and list.
	public readonly T[] items;

	/// Position (1-index-based) of current page
	public readonly int pagePos;

	/// Total number of page
	public readonly int pageCount;

	/// Total item count
	public readonly int totalItemCount;

	public PagedResult(T[] items, int pagePos, int pageCount, int totalItemCount) {
		this.items = items;
		this.pagePos = pagePos;
		this.pageCount = pageCount;
		this.totalItemCount = totalItemCount;
	}
}
