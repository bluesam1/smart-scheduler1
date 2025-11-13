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
}

export default nextConfig
