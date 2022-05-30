# LightshipVPSDemos
Augmented Reality demos with Niantic Lightship ARDK VPS

Be sure to watch my [Lightship ARDK including VPS](https://www.youtube.com/playlist?list=PLQMQNmwN3FvxnT4KRXdYPGhPZ2kNWuOij) in YouTube.


## How to get started with this project ?

1. First you must create a new project by going to [Lightship Dev Portal](https://lightship.dev/account/projects) [follow this video](https://www.youtube.com/watch?v=RzC9whFj6rw&list=PLQMQNmwN3FvxnT4KRXdYPGhPZ2kNWuOij&index=2) if you want to watch a step by step process.
2. Update Assets/ArdkAuthConfig.asset with your app license


## Few considerations when using VPS:

1. Request Camera and Location Permissions in Your App
2. Set a UserId as a GUID since we will use users location information and it is a good practice
3. Use VPS Coverage API to Discover VPS-activated Wayspots
