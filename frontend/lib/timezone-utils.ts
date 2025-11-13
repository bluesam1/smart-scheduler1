/**
 * Estimates IANA timezone from coordinates using US timezone boundaries.
 * This is a simplified implementation for MVP.
 * For production, use TimeZoneDB API or Google Time Zone API with API key.
 */
export function estimateTimezoneFromCoordinates(latitude: number, longitude: number): string {
  // US timezone estimation (simplified)
  // Eastern: roughly -85 to -67 longitude
  // Central: roughly -102 to -85 longitude
  // Mountain: roughly -115 to -102 longitude
  // Pacific: roughly -125 to -115 longitude
  
  if (longitude >= -67 && longitude <= -50) {
    // Eastern US/Canada
    return "America/New_York"
  } else if (longitude >= -102 && longitude < -85) {
    // Central US/Canada
    return "America/Chicago"
  } else if (longitude >= -115 && longitude < -102) {
    // Mountain US/Canada
    return "America/Denver"
  } else if (longitude >= -125 && longitude < -115) {
    // Pacific US/Canada
    return "America/Los_Angeles"
  } else if (longitude < -125) {
    // Alaska
    return "America/Anchorage"
  } else if (longitude > -50) {
    // Atlantic/Eastern Canada
    return "America/Halifax"
  }
  
  // Default fallback
  return "America/New_York"
}


