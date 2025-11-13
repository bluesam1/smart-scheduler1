# Troubleshooting Google Places Autocomplete

## Important: Using Places API (New)

This component now uses the **Places API (New)** with `PlaceAutocompleteElement` instead of the legacy `Autocomplete` API. This is the recommended approach as of March 2025.

## Common Issues and Solutions

### Error: "Legacy API not enabled" or "You're calling a legacy API"

This error means you need to enable the **Places API (New)** instead of the legacy Places API.

#### Solution Steps:

1. **Go to Google Cloud Console**
   - Visit: https://console.cloud.google.com/
   - Select your project

2. **Enable Maps JavaScript API**
   - Go to: https://console.cloud.google.com/apis/library/maps-javascript-api.googleapis.com
   - Click **"Enable"** button
   - Wait for it to activate (usually instant, but can take a few minutes)

3. **Enable Places API (New)** ⚠️ **IMPORTANT: Use the NEW API**
   - Go to: https://console.cloud.google.com/apis/library/places-backend.googleapis.com
   - Click **"Enable"** button
   - Wait for activation
   - **Note:** Make sure you're enabling "Places API (New)" not the legacy "Places API"

4. **Verify API Key Restrictions**
   - Go to: https://console.cloud.google.com/apis/credentials
   - Click on your API key
   - Under "API restrictions":
     - If "Don't restrict key" is selected → This should work
     - If "Restrict key" is selected → Make sure both "Maps JavaScript API" and "Places API (New)" are checked

5. **Check HTTP Referrer Restrictions** (if any)
   - In the same API key settings
   - Under "Application restrictions"
   - If "HTTP referrers" is selected, make sure `localhost:3000` is in the allowed list
   - For development, you can add: `http://localhost:3000/*`

6. **Restart Your Dev Server**
   ```bash
   # Stop the server (Ctrl+C) and restart
   cd frontend
   npm run dev
   ```

### Check Browser Console

Open your browser's developer console (F12) and look for:

1. **API Key Logs:**
   ```
   [GooglePlacesAutocomplete] API Key present: true
   [GooglePlacesAutocomplete] API Key length: 39
   ```

2. **Loading Status:**
   ```
   [GooglePlacesAutocomplete] Script tag added to document head
   [GooglePlacesAutocomplete] Script onload fired
   [GooglePlacesAutocomplete] Google Maps callback executed
   [GooglePlacesAutocomplete] Places API available after callback
   ```

3. **Error Messages:**
   - If you see "ApiNotActivatedMapError" → Enable Maps JavaScript API
   - If you see "This API project is not authorized" → Check API key restrictions
   - If you see "RefererNotAllowedMapError" → Add your domain to HTTP referrer restrictions

### Verify Environment Variable

1. **Check `.env.local` file exists:**
   ```bash
   cd frontend
   ls -la .env.local
   ```

2. **Verify the key is set:**
   ```bash
   cat .env.local | grep GOOGLE
   ```

3. **Make sure there are no extra spaces or quotes:**
   ```env
   # ✅ Correct
   NEXT_PUBLIC_GOOGLE_PLACES_API_KEY=AIzaSy...

   # ❌ Wrong (quotes)
   NEXT_PUBLIC_GOOGLE_PLACES_API_KEY="AIzaSy..."

   # ❌ Wrong (spaces)
   NEXT_PUBLIC_GOOGLE_PLACES_API_KEY = AIzaSy...
   ```

### Test API Key Directly

You can test your API key by visiting this URL in your browser (replace `YOUR_API_KEY`):

```
https://maps.googleapis.com/maps/api/js?key=YOUR_API_KEY&libraries=places
```

- **If it works:** You'll see JavaScript code (this is normal)
- **If you see an error:** The error message will tell you what's wrong

### Quick Checklist

- [ ] `.env.local` file exists in `frontend/` directory
- [ ] `NEXT_PUBLIC_GOOGLE_PLACES_API_KEY` is set (no quotes, no spaces)
- [ ] Maps JavaScript API is enabled in Google Cloud Console
- [ ] Places API is enabled in Google Cloud Console
- [ ] API key has correct restrictions (or no restrictions for dev)
- [ ] HTTP referrer restrictions allow `localhost:3000` (if restrictions are set)
- [ ] Dev server was restarted after adding the key
- [ ] Browser console shows the API key is present
- [ ] No errors in browser console related to Google Maps

### Still Not Working?

1. **Clear browser cache** and hard refresh (Ctrl+Shift+R)
2. **Check the Network tab** in browser DevTools:
   - Look for the request to `maps.googleapis.com`
   - Check the response - it should be JavaScript, not an error
3. **Try in an incognito window** to rule out browser extensions
4. **Check Google Cloud Console billing** - some APIs require billing to be enabled
5. **Wait 5-10 minutes** after enabling APIs - sometimes there's a propagation delay

