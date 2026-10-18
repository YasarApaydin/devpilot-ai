/**
 * Formats a file size in bytes to a human-readable string (KB, MB, GB).
 */
export function formatFileSize(bytes: number): string {
  if (bytes <= 0 || isNaN(bytes)) return '0 B';

  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const digitGroups = Math.min(
    Math.floor(Math.log(bytes) / Math.log(1024)),
    units.length - 1
  );

  const value = bytes / Math.pow(1024, digitGroups);
  return `${value.toFixed(value >= 10 || digitGroups === 0 ? 0 : 1)} ${units[digitGroups]}`;
}
