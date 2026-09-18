vcl 4.1;

import std;

# Varnish Configuration for dagangOnline (ASP.NET Core & Blazor Server)
# Caches static files and public pages while passing WebSockets (SignalR) and authenticated sessions.

backend default {
    .host = "127.0.0.1";
    .port = "5000";
    .connect_timeout = 5s;
    .first_byte_timeout = 60s;
    .between_bytes_timeout = 10s;
}

sub vcl_recv {
    # 1. Normalize Host header
    set req.http.Host = regsub(req.http.Host, ":[0-9]+$", "");

    # 2. WebSocket bypass for Blazor Server & SignalR Hubs
    if (req.http.Upgrade ~ "(?i)websocket" || 
        req.url ~ "^/_blazor" || 
        req.url ~ "^/supportChatHub") {
        return (pipe);
    }

    # 3. Only cache GET and HEAD requests
    if (req.method != "GET" && req.method != "HEAD") {
        return (pass);
    }

    # 4. Bypass cache for authenticated user sessions and admin/account portals
    if (req.http.Cookie ~ "dagangOnline\." || 
        req.http.Cookie ~ "\.AspNetCore\." ||
        req.url ~ "^/Admin" || 
        req.url ~ "^/Account" || 
        req.url ~ "^/Dashboard" ||
        req.url ~ "^/Agent") {
        return (pass);
    }

    # 5. Static Assets (Images, Videos, Fonts, CSS, JS, Onsen UI)
    if (req.url ~ "\.(css|js|woff2?|ttf|eot|svg|png|jpg|jpeg|gif|ico|webp|mp4|webm|webmanifest)(\?.*)?$") {
        # Strip cookies on static assets to maximize hit rate
        unset req.http.Cookie;
        return (hash);
    }

    # 6. Normalize Accept-Encoding for gzip/br
    if (req.http.Accept-Encoding) {
        if (req.http.Accept-Encoding ~ "gzip") {
            set req.http.Accept-Encoding = "gzip";
        } elsif (req.http.Accept-Encoding ~ "deflate") {
            set req.http.Accept-Encoding = "deflate";
        } else {
            unset req.http.Accept-Encoding;
        }
    }

    return (hash);
}

sub vcl_pipe {
    # WebSocket support requires Connection: Upgrade
    if (req.http.upgrade) {
        set bereq.http.upgrade = req.http.upgrade;
    }
    return (pipe);
}

sub vcl_backend_response {
    # 1. Cache static assets for 7 days
    if (bereq.url ~ "\.(css|js|woff2?|ttf|eot|svg|png|jpg|jpeg|gif|ico|webp|mp4|webm|webmanifest)(\?.*)?$") {
        unset beresp.http.set-cookie;
        set beresp.ttl = 7d;
        set beresp.grace = 1d;
        set beresp.http.Cache-Control = "public, max-age=604800, immutable";
    }

    # 2. Allow caching of public pages when Surrogate-Control or Cache-Control permits
    if (beresp.http.Surrogate-Control ~ "max-age" || beresp.http.Cache-Control ~ "public") {
        unset beresp.http.set-cookie;
        set beresp.grace = 6h;
    }

    # 3. Add debug header to verify Varnish response
    set beresp.http.X-Varnish-Server = server.hostname;

    return (deliver);
}

sub vcl_deliver {
    # Add X-Cache HIT/MISS diagnostic header for client testing
    if (obj.hits > 0) {
        set resp.http.X-Cache = "HIT";
        set resp.http.X-Cache-Hits = obj.hits;
    } else {
        set resp.http.X-Cache = "MISS";
    }

    return (deliver);
}
