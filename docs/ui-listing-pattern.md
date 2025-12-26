# UI Listing Pattern

This document describes the shared "List Page" pattern used across Explore, Plan, and Admin list pages.

## Structure

- `PageShell` wraps the page with `container-app` and consistent spacing.
- `PageHeader` renders title/subtitle + optional actions.
- `ListToolbar` holds filters (left) and sort/actions (right). It can show a conditional "Temizle".
- `ActiveFiltersBar` shows removable filter chips and an optional "Clear all".
- `ResultsHeader` displays "{count} sonuç bulundu" and optional sort label.
- `ListPagination` shows range + Prev/Next only when needed.
- `ListEmptyState` provides a themed empty state block.

## Query Params (Source of Truth)

Use `useListQueryParams` to read/write URL params and debounce fetches:

```ts
const {
  searchParams,
  debouncedSearchParams,
  getParam,
  getNumberParam,
  updateParams,
  clearParams,
  hasActiveFilters,
} = useListQueryParams({
  filterKeys: ["city", "from", "to"],
  defaults: { tab: "upcoming", sort: "date", pageSize: 20 },
});
```

- Update params via `updateParams({ city: "Istanbul" })`.
- Remove filters by passing `""` or `null`.
- `updateParams` resets `page` when filters change.
- Fetch with `debouncedSearchParams` (300–500ms).

## Example

```tsx
<PageShell>
  <PageHeader title="..." subtitle="..." />
  <ListToolbar
    left={<Filters />}
    right={<SortSelect />}
    showClear={hasActiveFilters}
    clearLabel={t("filters.clear")}
    onClear={() => clearParams()}
  />
  <ActiveFiltersBar
    filters={activeFilters}
    onClearAll={() => clearParams()}
    clearLabel={t("filters.clear")}
  />
  <ResultsHeader count={total} />
  <ListPagination page={page} pageSize={pageSize} total={total} onPageChange={...} />
</PageShell>
```
