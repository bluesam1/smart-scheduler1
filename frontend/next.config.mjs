/** @type {import('next').NextConfig} */
const nextConfig = {
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  // Enable static export for AWS Amplify hosting
  output: 'export',
  // Disable features that require server-side rendering
  trailingSlash: true,
  // Ensure asset paths are correct for static export
  assetPrefix: process.env.NODE_ENV === 'production' ? '' : undefined,
}

export default nextConfig
