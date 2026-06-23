(function() {
    'use strict';

    console.log('[UserRatings] Loading plugin...');

    // CSS for inline ratings UI
    // Uses ElegantFin CSS custom properties with fallbacks for default Jellyfin
    const style = document.createElement('style');
    style.textContent = `
        #user-ratings-ui {
            grid-column: 1 / -1;
            min-height: 1px;
        }
        .user-ratings-container {
            background: var(--lighterGradientPointAlpha, rgba(0, 0, 0, 0.15));
            backdrop-filter: var(--blurDefault, blur(10px));
            border-radius: var(--largeRadius, 10px);
            padding: 1.8em 2em;
            margin-top: 1.5em;
            margin-bottom: 1.5em;
            border: var(--defaultBorder, 1px solid rgba(255, 255, 255, 0.08));
            box-sizing: border-box;
            color: var(--textColor, #ffffff);
        }
        .user-ratings-container * {
            box-sizing: border-box;
        }
        .user-ratings-header {
            font-size: 1.3em;
            font-weight: 500;
            margin-bottom: 1.2em;
            color: var(--textColor, #ffffff);
            display: flex;
            align-items: center;
            gap: 1em;
            flex-wrap: wrap;
        }
        .user-ratings-average {
            color: #ffd700;
            font-size: 1.1em;
        }
        .user-ratings-my-rating {
            margin-bottom: 1.5em;
            padding-bottom: 1.5em;
            border-bottom: var(--defaultBorder, 1px solid rgba(255, 255, 255, 0.08));
        }
        .user-ratings-section-title {
            font-size: 1.15em;
            margin-bottom: 0.3em;
            color: var(--textColor, #ffffff);
            font-weight: 600;
        }
        .user-ratings-section-subtitle {
            font-size: 0.9em;
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
            margin-bottom: 0.8em;
        }
        .rating-form-row {
            display: block;
            margin-top: 0.5em;
        }
        .rating-form-section {
            margin-bottom: 1.6em;
        }
        .star-rating-container {
            display: flex;
            align-items: center;
            gap: 0.8em;
            margin-bottom: 0.5em;
        }
        .star-rating {
            display: inline-flex;
            gap: 0.25em;
            cursor: pointer;
            font-size: 1.9em;
        }
        .star-rating .star {
            color: rgba(255, 255, 255, 0.15);
            transition: color 0.2s, transform 0.15s;
            cursor: pointer;
            filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.3));
        }
        .star-rating .star.filled {
            color: #ffd700;
        }
        .star-rating .star.half {
            background: linear-gradient(90deg, #ffd700 50%, rgba(255, 255, 255, 0.15) 50%);
            -webkit-background-clip: text;
            background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        .star-rating .star:hover {
            color: #ffed4e;
            transform: scale(1.15);
        }
        .star-rating .star.half:hover {
            background: linear-gradient(90deg, #ffed4e 50%, rgba(255, 255, 255, 0.15) 50%);
            -webkit-background-clip: text;
            background-clip: text;
            -webkit-text-fill-color: transparent;
            transform: scale(1.15);
        }
        .rating-prompt {
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
            font-size: 0.9em;
        }
        .rating-note-input {
            width: 100%;
            padding: 0.5em;
            background: var(--selectorBackgroundColor, rgba(0, 0, 0, 0.2));
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.2));
            border-radius: var(--smallRadius, 8px);
            color: var(--textColor, #ffffff);
            font-size: 0.95em;
            font-family: inherit;
            transition: border-color 0.2s, background 0.2s;
            resize: vertical;
            min-height: 100px;
            line-height: 1.6;
        }
        .rating-note-input:focus {
            outline: none;
            border-color: var(--highlightOutlineColor, #00a4dc) !important;
            background: var(--selectorBackgroundColor, rgba(0, 0, 0, 0.3));
            box-shadow: 0 0 0 1px var(--highlightOutlineColor, #00a4dc);
        }
        .rating-note-input::placeholder {
            color: var(--dimTextColor, rgba(255, 255, 255, 0.35));
        }
        .rating-char-count {
            font-size: 0.85em;
            color: var(--dimTextColor, rgba(255, 255, 255, 0.4));
            margin-top: 0.5em;
        }
        .rating-char-count.error {
            color: #ff6b6b;
        }
        .rating-actions {
            margin-top: 1.2em;
            display: flex;
            gap: 0.75em;
            flex-wrap: wrap;
        }
        .rating-actions button {
            position: relative;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            box-sizing: border-box;
            outline: 0;
            margin: 0;
            cursor: pointer;
            user-select: none;
            vertical-align: middle;
            text-decoration: none;
            font-family: inherit;
            font-weight: 500;
            font-size: 0.9375rem;
            line-height: 1.75;
            letter-spacing: 0.02857em;
            text-transform: uppercase;
            min-width: 64px;
            padding: 8px 22px;
            border-radius: var(--smallRadius, 4px);
            transition: background-color 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms, box-shadow 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms, border-color 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms, filter 125ms;
        }
        .rating-actions .save-btn {
            background-color: var(--btnSubmitColor, #e53935);
            color: #fff;
            border: solid var(--btnSubmitBorderColor, transparent) var(--borderWidth, 0.06em);
            box-shadow: 0px 3px 1px -2px rgba(0,0,0,0.2), 0px 2px 2px 0px rgba(0,0,0,0.14), 0px 1px 5px 0px rgba(0,0,0,0.12);
            flex: 1;
            min-width: 200px;
        }
        .rating-actions .save-btn:hover:not(:disabled) {
            filter: brightness(1.2);
        }
        .rating-actions .save-btn:active:not(:disabled) {
            filter: brightness(1.2);
            transform: scale(0.98);
        }
        .rating-actions .save-btn:disabled {
            background-color: rgba(255, 255, 255, 0.12);
            color: rgba(255, 255, 255, 0.3);
            cursor: default;
            pointer-events: none;
            box-shadow: none;
            filter: none;
        }
        .rating-actions .delete-btn {
            background-color: var(--btnDeleteColor, transparent);
            color: var(--textColor, rgba(255, 255, 255, 0.7));
            border: solid var(--btnDeleteBorderColor, rgba(255, 255, 255, 0.23)) var(--borderWidth, 0.06em);
            padding: 7px 21px;
        }
        .rating-actions .delete-btn:hover {
            filter: brightness(1.2);
            background-color: var(--btnDeleteColor, rgba(255, 255, 255, 0.08));
        }
        .rating-actions .delete-btn:active {
            filter: brightness(1.2);
            transform: scale(0.98);
        }
        .user-ratings-all {
            margin-top: 1.5em;
        }
        .rating-item {
            margin: 0.75em 0;
            padding: 1em;
            background: var(--darkerGradientPointAlpha, rgba(0, 0, 0, 0.12));
            border-radius: var(--smallRadius, 8px);
            color: var(--textColor, #ffffff);
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.05));
        }
        .rating-item-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 0.5em;
            flex-wrap: wrap;
            gap: 0.5em;
        }
        .rating-item-user {
            font-weight: 500;
            color: var(--textColor, #ffffff);
        }
        .rating-item-stars {
            color: #ffd700;
            margin-left: 0.5em;
        }
        .rating-item-date {
            font-size: 0.85em;
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
        }
        .rating-item-note {
            margin-top: 0.5em;
            opacity: 0.9;
            font-size: 0.95em;
            color: var(--dimTextColor, #e0e0e0);
            line-height: 1.4;
        }
        .user-ratings-my-rating.collapsed .rating-form-section,
        .user-ratings-my-rating.collapsed .rating-actions {
            display: none;
        }
        .my-rating-summary {
            display: none;
            align-items: center;
            gap: 0.6em;
            cursor: pointer;
            padding: 0.6em 0.9em;
            border-radius: var(--smallerRadius, 6px);
            transition: background 0.2s;
            margin-left: auto;
            flex-wrap: wrap;
        }
        .my-rating-summary:hover {
            background: rgba(255, 255, 255, 0.06);
        }
        .user-ratings-container.has-rating .my-rating-summary {
            display: flex;
        }
        .summary-stars {
            color: #ffd700;
            font-size: 1.1em;
            letter-spacing: 1px;
        }
        .summary-label {
            color: var(--dimTextColor, rgba(255, 255, 255, 0.6));
            font-size: 0.95em;
        }
        .edit-rating-btn {
            margin-left: auto;
            background: transparent;
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.23));
            color: var(--dimTextColor, rgba(255, 255, 255, 0.7));
            padding: 4px 14px;
            border-radius: var(--smallerRadius, 4px);
            cursor: pointer;
            font-size: 0.85em;
            font-family: inherit;
            transition: background 0.2s, border-color 0.2s;
        }
        .edit-rating-btn:hover {
            background: rgba(255, 255, 255, 0.08);
            border-color: var(--lighterBorderColor, rgba(255, 255, 255, 0.3));
        }
        .collapse-rating-btn {
            display: none;
            background: none;
            border: var(--defaultLighterBorder, 1px solid rgba(255, 255, 255, 0.23));
            color: var(--dimTextColor, rgba(255, 255, 255, 0.5));
            padding: 2px 10px;
            border-radius: var(--smallerRadius, 4px);
            cursor: pointer;
            font-size: 0.8em;
            font-family: inherit;
            margin-left: 0.5em;
            transition: background 0.2s, color 0.2s;
        }
        .collapse-rating-btn:hover {
            background: rgba(255, 255, 255, 0.06);
            color: var(--textColor, rgba(255, 255, 255, 0.8));
        }
        .user-ratings-container.has-rating .collapse-rating-btn {
            display: inline-block;
        }
        @media (max-width: 480px) {
            .user-ratings-container {
                padding: 1em 0.8em;
                margin-top: 1em;
                margin-bottom: 1em;
                border-radius: var(--smallRadius, 8px);
            }
            .user-ratings-header {
                font-size: 1.1em;
                gap: 0.5em;
            }
            .my-rating-summary {
                margin-left: 0;
                flex-direction: column;
                align-items: flex-start;
                gap: 0.3em;
                width: 100%;
                padding: 0.5em 0.7em;
            }
            .edit-rating-btn {
                margin-left: 0;
                width: 100%;
                text-align: center;
                margin-top: 0.2em;
            }
            .star-rating {
                font-size: 1.5em;
            }
            .rating-actions {
                flex-direction: column;
                gap: 0.5em;
            }
            .rating-actions .save-btn,
            .rating-actions .delete-btn {
                min-width: unset;
                flex: unset;
                width: 100%;
                text-align: center;
                padding: 8px 22px;
            }
            .rating-note-input {
                min-height: 80px;
                padding: 0.8em;
            }
            .rating-item {
                padding: 0.75em;
            }
        }

        /* ===== RATE BADGE (unrated cards) ===== */
        .rate-badge {
            background: linear-gradient(135deg, #43a047, #2e7d32);
            border: 1px solid rgba(255,255,255,0.15);
            border-radius: 6px;
            padding: 0.35em 0.8em;
            display: inline-flex;
            align-items: center;
            gap: 0.3em;
            cursor: pointer;
            font-weight: 600;
            font-size: 0.85em;
            color: #fff;
            box-shadow: 0 2px 6px rgba(0,0,0,0.3);
            transition: transform 0.12s ease, box-shadow 0.12s ease, background 0.12s ease;
            pointer-events: auto;
            user-select: none;
        }
        .rate-badge:hover {
            background: linear-gradient(135deg, #4caf50, #388e3c);
            transform: translateY(-1px) scale(1.05);
            box-shadow: 0 4px 12px rgba(76,175,80,0.4);
            border-color: rgba(255,255,255,0.3);
        }
        .rate-badge:active {
            transform: translateY(0) scale(0.98);
            box-shadow: 0 1px 3px rgba(0,0,0,0.3);
        }

        /* ===== COMPACT RATING BADGE (rated cards) ===== */
        .compact-rating {
            position: absolute;
            top: 0.4em; left: 0.4em;
            z-index: 3;
            display: inline-flex;
            align-items: center;
            gap: 0.25em;
            background: rgba(0,0,0,0.7);
            padding: 0.2em 0.5em;
            border-radius: 3px;
            font-size: 0.8em;
            line-height: 1.5;
            cursor: pointer;
            pointer-events: auto;
            transition: background 0.15s;
        }
        .compact-rating:hover {
            background: rgba(0,0,0,0.9);
        }
        .compact-rating .cr-star {
            color: #ffd700;
            font-size: 1.1em;
        }
        .compact-rating .cr-value {
            font-weight: 600;
            color: #fff;
        }
        .compact-rating .cr-edit {
            opacity: 0;
            transition: opacity 0.2s;
            color: rgba(255,255,255,0.4);
            font-size: 0.85em;
            margin-left: 0.15em;
        }
        .card-hoverable:hover .compact-rating .cr-edit {
            opacity: 1;
        }
        .compact-rating[data-empty="true"] {
            display: none;
        }

        /* ===== RATE SUCCESS ANIMATION ===== */
        @keyframes rate-success-pulse {
            0% { box-shadow: 0 0 0 0 rgba(255,215,0,0.6); }
            50% { box-shadow: 0 0 0 6px rgba(255,215,0,0.2); }
            100% { box-shadow: 0 0 0 0 rgba(255,215,0,0); }
        }
        .card-rating-success {
            animation: rate-success-pulse 0.6s ease-out;
        }

        /* ===== RATE POPUP MODAL ===== */
        .rate-popup-overlay {
            display: none;
            position: fixed;
            inset: 0;
            background: rgba(0,0,0,0.6);
            z-index: 10000;
            align-items: center;
            justify-content: center;
        }
        .rate-popup-overlay.open {
            display: flex;
        }
        .rate-popup {
            background: #1e1e1e;
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 14px;
            padding: 1.6em 1.8em 1.4em;
            width: 380px;
            max-width: 92vw;
            box-shadow: 0 12px 40px rgba(0,0,0,0.5);
            animation: popup-slide-in 0.2s ease-out;
        }
        @keyframes popup-slide-in {
            from { opacity: 0; transform: scale(0.95) translateY(12px); }
            to { opacity: 1; transform: scale(1) translateY(0); }
        }
        .rate-popup-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 0.4em;
        }
        .rate-popup-title {
            font-size: 1.1em;
            font-weight: 600;
            color: var(--textColor, #fff);
        }
        .rate-popup-close {
            background: none;
            border: none;
            color: rgba(255,255,255,0.4);
            font-size: 1.4em;
            cursor: pointer;
            padding: 0 0.2em;
            line-height: 1;
        }
        .rate-popup-close:hover {
            color: #fff;
        }
        .rate-popup-subtitle {
            font-size: 0.85em;
            color: var(--dimTextColor, rgba(255,255,255,0.5));
            margin-bottom: 1em;
        }
        .rate-popup-stars {
            display: flex;
            gap: 0.3em;
            font-size: 2em;
            justify-content: center;
            margin-bottom: 1em;
        }
        .rate-popup-stars .rp-star {
            background: linear-gradient(90deg, #ffd700 var(--star-fill, 0%), rgba(255,255,255,0.15) var(--star-fill, 0%));
            -webkit-background-clip: text;
            background-clip: text;
            color: transparent;
            cursor: pointer;
            transition: transform 0.12s;
        }
        .rate-popup-stars .rp-star.hover-highlight {
            filter: brightness(1.25);
        }
        .rate-popup-stars .rp-star:hover {
            transform: scale(1.15);
        }
        .rate-popup textarea {
            width: 100%;
            padding: 0.6em;
            background: rgba(0,0,0,0.3);
            border: 1px solid rgba(255,255,255,0.12);
            border-radius: 8px;
            color: var(--textColor, #ddd);
            font-size: 0.9em;
            font-family: inherit;
            resize: vertical;
            min-height: 70px;
            margin-bottom: 1em;
            line-height: 1.5;
        }
        .rate-popup textarea:focus {
            outline: none;
            border-color: var(--highlightOutlineColor, #00a4dc);
        }
        .rate-popup-actions {
            display: flex;
            gap: 0.6em;
            justify-content: flex-end;
        }
        .rate-popup-actions button {
            padding: 0.5em 1.4em;
            border-radius: 6px;
            border: none;
            font-size: 0.9em;
            font-weight: 500;
            cursor: pointer;
            font-family: inherit;
            transition: filter 0.15s;
        }
        .rate-popup-actions button:hover {
            filter: brightness(1.15);
        }
        .rate-popup-btn-cancel {
            background: rgba(255,255,255,0.08);
            color: var(--textColor, #ddd);
            border: 1px solid rgba(255,255,255,0.15) !important;
        }
        .rate-popup-btn-submit {
            background: var(--btnSubmitColor, #e53935);
            color: #fff;
            min-width: 120px;
        }
        .rate-popup-btn-submit:disabled {
            opacity: 0.4;
            cursor: default;
            filter: none;
        }
        .rate-popup-btn-delete {
            background: transparent;
            color: #e53935;
            border: 1px solid rgba(229,57,53,0.3) !important;
            margin-right: auto;
        }
        .rate-popup-btn-delete:hover {
            background: rgba(229,57,53,0.1);
        }
        @media (max-width: 480px) {
            .rate-popup {
                padding: 1.2em;
            }
            .rate-popup-stars {
                font-size: 1.6em;
            }
        }
    `;
    document.head.appendChild(style);

    let currentItemId = null;
    let currentRating = 0;
    let isInjecting = false;
    let userRatingsMap = null;
    let popupModal = null;
    let popupActiveItemId = null;

function createStarRating(rating, interactive, onHover, onClick) {
    const container = document.createElement('div');
    container.className = 'star-rating';
    let currentSelectedRating = rating;
    
    for (let i = 1; i <= 5; i++) {
        const star = document.createElement('span');
        star.className = 'star';
        star.textContent = '\u2605';
        star.dataset.position = i;
        
        if (interactive) {
            star.addEventListener('mousemove', (e) => {
                const rect = star.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const isRightHalf = x > rect.width / 2;
                const hoverRating = (i - 1) * 2 + (isRightHalf ? 2 : 1);
                if (onHover) onHover(hoverRating);
            });
            star.addEventListener('click', (e) => {
                const rect = star.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const isRightHalf = x > rect.width / 2;
                currentSelectedRating = (i - 1) * 2 + (isRightHalf ? 2 : 1);
                if (onClick) onClick(currentSelectedRating);
            });
        }
        
        container.appendChild(star);
    }
    
    if (interactive) {
        container.addEventListener('mouseleave', () => {
            if (onHover) onHover(currentSelectedRating);
        });
    }
    
    updateStarDisplay(container, rating);
    return container;
}

function updateStarDisplay(container, rating) {
    const stars = container.querySelectorAll('.star');
    stars.forEach((star, index) => {
        const starPos = index + 1;
        star.classList.remove('filled', 'half');
        
        if (starPos <= Math.floor(rating / 2)) {
            star.classList.add('filled');
        } else if (starPos === Math.ceil(rating / 2) && rating % 2 === 1) {
            star.classList.add('half');
        }
    });
}

    async function loadRatings(itemId) {
        try {
            const response = await fetch(ApiClient.getUrl(`api/UserRatings/Item/${itemId}`), {
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            const data = await response.json();
            console.log('[UserRatings] Loaded ratings:', data);
            return data;
        } catch (error) {
            console.error('[UserRatings] Error loading ratings:', error);
            return { ratings: [], averageRating: 0, totalRatings: 0 };
        }
    }

    async function loadMyRating(itemId) {
        try {
            const userId = ApiClient.getCurrentUserId();
            const response = await fetch(ApiClient.getUrl(`api/UserRatings/MyRating/${itemId}?userId=${userId}`), {
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            const data = await response.json();
            return data;
        } catch (error) {
            console.error('[UserRatings] Error loading my rating:', error);
            return null;
        }
    }

    async function saveRating(itemId, rating, note) {
        try {
            const userId = ApiClient.getCurrentUserId();
            const user = await ApiClient.getCurrentUser();
            const userName = user ? user.Name : 'Unknown';
            const url = ApiClient.getUrl(`api/UserRatings/Rate?itemId=${itemId}&userId=${userId}&rating=${rating}${note ? '&note=' + encodeURIComponent(note) : ''}&userName=${encodeURIComponent(userName)}`);
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            
            if (!response.ok) {
                const text = await response.text();
                console.error('[UserRatings] Server error:', response.status, text);
                return { success: false, message: `Server error: ${response.status}` };
            }
            
            return await response.json();
        } catch (error) {
            console.error('[UserRatings] Error saving rating:', error);
            return { success: false, message: error.message };
        }
    }

    async function deleteRating(itemId) {
        try {
            const userId = ApiClient.getCurrentUserId();
            const url = ApiClient.getUrl(`api/UserRatings/Rating?itemId=${itemId}&userId=${userId}`);
            const response = await fetch(url, {
                method: 'DELETE',
                headers: {
                    'X-Emby-Token': ApiClient.accessToken()
                }
            });
            return await response.json();
        } catch (error) {
            console.error('[UserRatings] Error deleting rating:', error);
            return { success: false, message: error.message };
        }
    }

    // ===== Phase 1: Hover-to-Rate =====

    let _popupCardElement = null;

    async function fetchUserRatings() {
        try {
            const userId = ApiClient.getCurrentUserId();
            const resp = await fetch(ApiClient.getUrl(`api/UserRatings/User/${userId}`), {
                headers: { 'X-Emby-Token': ApiClient.accessToken() }
            });
            if (!resp.ok) { userRatingsMap = {}; return; }
            const data = await resp.json();
            userRatingsMap = {};
            if (data.ratings && data.ratings.length > 0) {
                data.ratings.forEach(r => {
                    userRatingsMap[r.itemId] = { rating: r.rating, note: r.note };
                });
            }
        } catch (e) {
            console.error('[UserRatings] Error fetching user ratings:', e);
            userRatingsMap = {};
        }
    }

    function getUserRating(itemId) {
        return userRatingsMap && userRatingsMap[itemId] ? userRatingsMap[itemId] : null;
    }

    function formatStarRating(val) {
        return val === Math.floor(val) ? val + '/5' : val.toFixed(1) + '/5';
    }

    function setPopupStarFill(stars, value) {
        stars.forEach(function(s, idx) {
            var starNum = idx + 1;
            var fillPct = 0;
            if (value >= starNum) {
                fillPct = 100;
            } else if (starNum - value === 0.5) {
                fillPct = 50;
            }
            s.style.setProperty('--star-fill', fillPct + '%');
        });
    }

    function createRatePopupModal() {
        if (popupModal) return popupModal;

        const overlay = document.createElement('div');
        overlay.className = 'rate-popup-overlay';
        overlay.id = 'ratePopupOverlay';
        overlay.innerHTML = `
            <div class="rate-popup">
                <div class="rate-popup-header">
                    <span class="rate-popup-title" id="rpTitle">Rate</span>
                    <button class="rate-popup-close" id="rpClose">&#xD7;</button>
                </div>
                <div class="rate-popup-subtitle" id="rpSubtitle">Select your rating</div>
                <div class="rate-popup-stars" id="rpStars">
                    <span class="rp-star" data-n="1">&#x2605;</span>
                    <span class="rp-star" data-n="2">&#x2605;</span>
                    <span class="rp-star" data-n="3">&#x2605;</span>
                    <span class="rp-star" data-n="4">&#x2605;</span>
                    <span class="rp-star" data-n="5">&#x2605;</span>
                </div>
                <textarea id="rpNote" placeholder="Optional note&#x2026;"></textarea>
                <div class="rate-popup-actions">
                    <button class="rate-popup-btn-delete" id="rpDelete" style="display:none;">Delete</button>
                    <div style="display:flex;gap:0.6em;margin-left:auto;">
                        <button class="rate-popup-btn-cancel" id="rpCancel">Cancel</button>
                        <button class="rate-popup-btn-submit" id="rpSubmit">Post Rating</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        popupModal = overlay;

        // Star hover/click in popup
        const popupStars = overlay.querySelectorAll('.rp-star');
        popupStars.forEach(function(star) {
            star.addEventListener('mouseenter', function(e) {
                var n = parseFloat(this.dataset.n);
                var rect = this.getBoundingClientRect();
                var isLeft = e.clientX < rect.left + rect.width / 2;
                var val = isLeft ? n - 0.5 : n;
                setPopupStarFill(popupStars, val);
                document.getElementById('rpSubtitle').textContent = 'Your rating: ' + formatStarRating(val);
                // Highlight hovered star
                popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
                this.classList.add('hover-highlight');
            });
            star.addEventListener('click', function(e) {
                var n = parseFloat(this.dataset.n);
                var rect = this.getBoundingClientRect();
                var isLeft = e.clientX < rect.left + rect.width / 2;
                var val = isLeft ? n - 0.5 : n;
                setPopupStarFill(popupStars, val);
                overlay.dataset.selectedRating = String(val);
                document.getElementById('rpSubtitle').textContent = 'Your rating: ' + formatStarRating(val);
                popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
            });
        });
        const popupStarsContainer = overlay.querySelector('#rpStars');
        popupStarsContainer.addEventListener('mouseleave', function() {
            var selected = parseFloat(overlay.dataset.selectedRating || '0');
            setPopupStarFill(popupStars, selected);
            popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
        });

        // Close handlers
        overlay.querySelector('#rpClose').addEventListener('click', closeRatePopup);
        overlay.querySelector('#rpCancel').addEventListener('click', closeRatePopup);
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) closeRatePopup();
        });

        // Submit handler
        overlay.querySelector('#rpSubmit').addEventListener('click', async function() {
            const itemId = popupActiveItemId;
            const selected = parseFloat(overlay.dataset.selectedRating || '0');
            if (selected === 0) return;

            const btn = this;
            btn.disabled = true;
            btn.textContent = 'Posting\u2026';

            const note = overlay.querySelector('#rpNote').value;
            const result = await saveRating(itemId, selected * 2, note);

            btn.disabled = false;
            btn.textContent = 'Post Rating';

            if (result.success) {
                const cardToAnimate = _popupCardElement;
                closeRatePopup();
                userRatingsMap[itemId] = { rating: selected * 2, note: note };
                try { sessionStorage.setItem('userRatingsDirty', 'true'); } catch (e) {}

                if (cardToAnimate) {
                    animateRatingSuccess(cardToAnimate, selected);
                }
            } else {
                alert('Error saving rating: ' + result.message);
            }
        });

        // Delete handler
        overlay.querySelector('#rpDelete').addEventListener('click', async function() {
            const itemId = popupActiveItemId;
            if (!confirm('Delete your rating?')) return;

            const btn = this;
            btn.disabled = true;
            btn.textContent = 'Deleting\u2026';

            const result = await deleteRating(itemId);

            btn.disabled = false;
            btn.textContent = 'Delete';

            if (result.success) {
                closeRatePopup();
                if (userRatingsMap) delete userRatingsMap[itemId];
                try { sessionStorage.setItem('userRatingsDirty', 'true'); } catch (e) {}
                location.reload();
            } else {
                alert('Error deleting rating: ' + result.message);
            }
        });

        return popupModal;
    }

    function openRatePopup(itemId, itemName, preselected, existingNote) {
        if (!popupModal) createRatePopupModal();
        popupActiveItemId = itemId;

        document.getElementById('rpTitle').textContent = 'Rate "' + (itemName || 'this item') + '"';
        document.getElementById('rpSubtitle').textContent = preselected > 0 ? 'Your rating: ' + formatStarRating(preselected) : 'Select your rating';
        document.getElementById('rpNote').value = existingNote || '';

        const popupStars = popupModal.querySelectorAll('.rp-star');
        setPopupStarFill(popupStars, preselected || 0);
        popupStars.forEach(function(s) { s.classList.remove('hover-highlight'); });
        popupModal.dataset.selectedRating = String(preselected || 0);

        const deleteBtn = popupModal.querySelector('#rpDelete');
        const submitBtn = popupModal.querySelector('#rpSubmit');
        const isEditing = preselected > 0;
        deleteBtn.style.display = isEditing ? 'inline-block' : 'none';
        submitBtn.textContent = isEditing ? 'Update' : 'Post Rating';

        popupModal.classList.add('open');
    }

    function closeRatePopup() {
        if (popupModal) {
            popupModal.classList.remove('open');
            popupModal.dataset.selectedRating = '0';
        }
        popupActiveItemId = null;
        _popupCardElement = null;
    }

    function attachRateButtonListeners(container) {
        const buttons = container.querySelectorAll('.rate-badge');
        buttons.forEach(function(btn) {
            if (btn._rateBtnAttached) return;
            btn._rateBtnAttached = true;

            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const cardEl = btn.closest('.card');
                if (!cardEl) return;
                const itemId = cardEl.getAttribute('data-id');
                const nameEl = cardEl.querySelector('.cardText a');
                const name = nameEl ? nameEl.textContent.trim() : null;
                _popupCardElement = cardEl;
                openRatePopup(itemId, name, 0, null);
            });
        });
    }

    function attachRatedCardListeners(container) {
        const badges = container.querySelectorAll('.compact-rating');
        badges.forEach(function(badge) {
            if (badge._rateEditAttached) return;
            badge._rateEditAttached = true;

            badge.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const card = badge.closest('.card');
                if (!card) return;
                const itemId = card.getAttribute('data-id');
                const nameEl = card.querySelector('.cardText a');
                const name = nameEl ? nameEl.textContent.trim() : null;
                const existing = getUserRating(itemId);
                const rating = existing ? existing.rating / 2 : 0;
                const note = existing ? (existing.note || '') : '';
                _popupCardElement = card;
                openRatePopup(itemId, name, rating, note);
            });
        });
    }

    function animateRatingSuccess(card, rating) {
        const imageContainer = card.querySelector('.cardImageContainer');
        if (imageContainer) {
            imageContainer.classList.remove('card-rating-success');
            void imageContainer.offsetWidth;
            imageContainer.classList.add('card-rating-success');
            setTimeout(function() {
                imageContainer.classList.remove('card-rating-success');
            }, 700);
        }

        // Change "Rate" badge to "★ N/5" on unrated cards (has rate-badge)
        const rateBadge = card.querySelector('.rate-badge');
        if (rateBadge) {
            rateBadge.innerHTML = '<span style="font-weight:600;font-size:0.9em;color:#ffd700;">\u2605 ' + rating + '/5</span>';
            rateBadge.classList.remove('rate-badge');
            rateBadge.removeAttribute('data-item-id');
        }

        // Show/update the compact rating badge
        let compactBadge = card.querySelector('.compact-rating');
        if (compactBadge) {
            compactBadge.querySelector('.cr-value').textContent = rating + '/5';
            compactBadge.dataset.empty = 'false';
            compactBadge.style.display = '';
        } else {
            compactBadge = document.createElement('div');
            compactBadge.className = 'compact-rating';
            compactBadge.dataset.empty = 'false';
            compactBadge.innerHTML = '<span class="cr-star">\u2605</span><span class="cr-value">' + rating + '/5</span><span class="cr-edit">\u270E</span>';
            compactBadge._rateEditAttached = false;
            const imgContainer = card.querySelector('.cardImageContainer');
            if (imgContainer && imgContainer.parentNode) {
                imgContainer.parentNode.insertBefore(compactBadge, imgContainer);
            } else {
                card.querySelector('.cardScalable').appendChild(compactBadge);
            }
            attachRatedCardListeners(card);
        }
    }

    function fillCompactBadges() {
        if (!userRatingsMap) return;
        document.querySelectorAll('#ratingsTab .compact-rating[data-item-id]').forEach(function(badge) {
            const itemId = badge.getAttribute('data-item-id');
            const userRating = getUserRating(itemId);
            if (userRating) {
                badge.querySelector('.cr-value').textContent = formatStarRating(userRating.rating / 2);
                badge.dataset.empty = 'false';
                badge.style.display = '';
            }
        });
    }

    function reInjectUI(itemId) {
        const existingUI = document.getElementById('user-ratings-ui');
        if (existingUI) {
            existingUI.remove();
        }
        isInjecting = false;
        if (itemId) {
            setTimeout(() => injectRatingsUI(), 150);
        }
    }

    async function createRatingsUI(itemId) {
        console.log('[UserRatings] → createRatingsUI started for:', itemId);
        const container = document.createElement('div');
        container.className = 'user-ratings-container';
        container.id = 'user-ratings-ui';
        
        // Get item name for personalized heading
        let itemName = 'this item';
        try {
            console.log('[UserRatings] → Loading item details...');
            const itemDetails = await ApiClient.getItem(ApiClient.getCurrentUserId(), itemId);
            if (itemDetails && itemDetails.Name) {
                itemName = itemDetails.Name;
            }
            console.log('[UserRatings] → Item details loaded:', itemName);
        } catch (error) {
            console.log('[UserRatings] Could not load item name:', error);
        }
        
        // Header
        const header = document.createElement('div');
        header.className = 'user-ratings-header';
        header.innerHTML = '<span>User Ratings</span>';
        
        const avgSpan = document.createElement('span');
        avgSpan.className = 'user-ratings-average';
        avgSpan.id = 'ratings-average-display';
        header.appendChild(avgSpan);
        
        const summaryEl = document.createElement('div');
        summaryEl.className = 'my-rating-summary';
        summaryEl.innerHTML = '<span class="summary-stars"></span><span class="summary-label">Your rating</span><button class="edit-rating-btn">Edit</button>';
        summaryEl.querySelector('.edit-rating-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            expandMyRating();
            summaryEl.style.display = 'none';
        });
        summaryEl.addEventListener('click', () => {
            expandMyRating();
            summaryEl.style.display = 'none';
        });
        header.appendChild(summaryEl);
        container.appendChild(header);
        
        // My Rating Section
        const myRatingSection = document.createElement('div');
        myRatingSection.className = 'user-ratings-my-rating';
        
        // Star Rating Section
        const starSection = document.createElement('div');
        starSection.className = 'rating-form-section';
        
        const myRatingTitle = document.createElement('div');
        myRatingTitle.className = 'user-ratings-section-title';
        myRatingTitle.textContent = `How would you rate ${itemName}?`;
        starSection.appendChild(myRatingTitle);
        
        const collapseBtn = document.createElement('button');
        collapseBtn.className = 'collapse-rating-btn';
        collapseBtn.textContent = '✕ Close';
        collapseBtn.addEventListener('click', () => {
            collapseMyRating();
            summaryEl.style.display = '';
        });
        
        const titleRow = document.createElement('div');
        titleRow.style.cssText = 'display:flex;align-items:center;';
        titleRow.appendChild(myRatingTitle);
        titleRow.appendChild(collapseBtn);
        starSection.appendChild(titleRow);
        
        const starRatingContainer = document.createElement('div');
        starRatingContainer.className = 'star-rating-container';
        
        const starContainer = createStarRating(0, true,
            (rating) => {
                updateStarDisplay(starContainer, rating);
                ratingPrompt.style.display = rating === 0 ? 'inline' : 'none';
            },
            (rating) => {
                currentRating = rating;
                updateStarDisplay(starContainer, rating);
                ratingPrompt.style.display = 'none';
            }
        );
        starRatingContainer.appendChild(starContainer);
        
        const ratingPrompt = document.createElement('span');
        ratingPrompt.className = 'rating-prompt';
        ratingPrompt.textContent = 'Select your rating';
        starRatingContainer.appendChild(ratingPrompt);
        
        starSection.appendChild(starRatingContainer);
        myRatingSection.appendChild(starSection);
        
        // Review Text Section
        const reviewSection = document.createElement('div');
        reviewSection.className = 'rating-form-section';
        
        const reviewTitle = document.createElement('div');
        reviewTitle.className = 'user-ratings-section-title';
        reviewTitle.textContent = 'Tell us about your experience';
        reviewSection.appendChild(reviewTitle);
        
        const reviewSubtitle = document.createElement('div');
        reviewSubtitle.className = 'user-ratings-section-subtitle';
        reviewSubtitle.textContent = 'Share your thoughts (optional)';
        reviewSection.appendChild(reviewSubtitle);
        
        const noteInput = document.createElement('textarea');
        noteInput.className = 'rating-note-input';
        noteInput.placeholder = 'Start your review...';
        reviewSection.appendChild(noteInput);
        
        const charCount = document.createElement('div');
        charCount.className = 'rating-char-count';
        charCount.textContent = '0 characters';
        reviewSection.appendChild(charCount);
        
        // Character counter
        noteInput.addEventListener('input', () => {
            const length = noteInput.value.length;
            charCount.textContent = `${length} character${length !== 1 ? 's' : ''}`;
        });
        
        myRatingSection.appendChild(reviewSection);
        
        // Actions
        const actionsContainer = document.createElement('div');
        actionsContainer.className = 'rating-actions';
        
        const saveBtn = document.createElement('button');
        saveBtn.className = 'save-btn';
        saveBtn.textContent = 'Post Rating';
        saveBtn.addEventListener('click', async () => {
            if (currentRating === 0) {
                alert('Please select a rating');
                return;
            }
            
            saveBtn.disabled = true;
            saveBtn.textContent = 'Posting...';
            
            const result = await saveRating(itemId, currentRating, noteInput.value);
            
            if (result.success) {
                saveBtn.textContent = 'Posted!';
                saveBtn.disabled = false;
                
                try { sessionStorage.setItem('userRatingsDirty', 'true'); } catch (e) {}
                
                await displayAllRatings(itemId, container);
                deleteBtn.style.display = 'inline-block';
                updateSummaryStars(currentRating);
                collapseMyRating();
                container.classList.add('has-rating');
                summaryEl.style.display = '';
            } else {
                alert('Error saving rating: ' + result.message);
                saveBtn.textContent = 'Post Rating';
                saveBtn.disabled = false;
            }
        });
        actionsContainer.appendChild(saveBtn);
        
        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'delete-btn';
        deleteBtn.textContent = 'Delete Rating';
        deleteBtn.style.display = 'none';
        deleteBtn.addEventListener('click', async () => {
            if (!confirm('Delete your rating?')) {
                return;
            }
            
            deleteBtn.disabled = true;
            deleteBtn.textContent = 'Deleting...';
            
            const result = await deleteRating(itemId);
            
            if (result.success) {
                currentRating = 0;
                noteInput.value = '';
                updateStarDisplay(starContainer, 0);
                deleteBtn.style.display = 'none';
                
                try { sessionStorage.setItem('userRatingsDirty', 'true'); } catch (e) {}
                
                await displayAllRatings(itemId, container);
                expandMyRating();
                summaryEl.style.display = 'none';
            } else {
                alert('Error deleting rating: ' + result.message);
            }
            
            deleteBtn.textContent = 'Delete Rating';
            deleteBtn.disabled = false;
        });
        actionsContainer.appendChild(deleteBtn);
        
        myRatingSection.appendChild(actionsContainer);
        
        const summaryStars = summaryEl.querySelector('.summary-stars');
        
function updateSummaryStars(rating) {
    if (summaryStars) {
        var fullStars = Math.floor(rating / 2);
        var hasHalf = rating % 2 === 1;
        var emptyStars = 5 - fullStars - (hasHalf ? 1 : 0);
        summaryStars.textContent = '\u2605'.repeat(fullStars) + (hasHalf ? '\u00BD' : '') + '\u2606'.repeat(emptyStars);
    }
}
        
        function collapseMyRating() {
            myRatingSection.classList.add('collapsed');
        }
        
        function expandMyRating() {
            myRatingSection.classList.remove('collapsed');
        }
        
        container.appendChild(myRatingSection);
        
        // All Ratings Section
        const allRatingsSection = document.createElement('div');
        allRatingsSection.className = 'user-ratings-all';
        allRatingsSection.id = 'all-ratings-section';
        container.appendChild(allRatingsSection);
        
        // Load existing rating
        console.log('[UserRatings] → Loading my rating...');
        const myRating = await loadMyRating(itemId);
        console.log('[UserRatings] → My rating loaded:', myRating);
        if (myRating && myRating.rating) {
            currentRating = myRating.rating;
            updateStarDisplay(starContainer, myRating.rating);
            ratingPrompt.style.display = 'none';
            noteInput.value = myRating.note || '';
            const length = noteInput.value.length;
            charCount.textContent = `${length} character${length !== 1 ? 's' : ''}`;
            deleteBtn.style.display = 'inline-block';
            updateSummaryStars(myRating.rating);
            collapseMyRating();
            container.classList.add('has-rating');
            summaryEl.style.display = '';
        } else {
            container.classList.remove('has-rating');
            summaryEl.style.display = 'none';
        }
        
        // Load all ratings
        console.log('[UserRatings] → Loading all ratings...');
        await displayAllRatings(itemId, container);
        console.log('[UserRatings] → All ratings loaded, returning container');
        
        return container;
    }

    async function displayAllRatings(itemId, container) {
        console.log('[UserRatings] → displayAllRatings started');
        const allRatingsSection = container.querySelector('#all-ratings-section');
        const avgDisplay = container.querySelector('#ratings-average-display');
        
        if (!allRatingsSection) {
            console.log('[UserRatings] → No allRatingsSection found, returning early');
            return;
        }
        
        allRatingsSection.innerHTML = '';
        
        console.log('[UserRatings] → Calling loadRatings...');
        const data = await loadRatings(itemId);
        console.log('[UserRatings] → loadRatings returned, processing data...');
        const ratings = data.ratings || [];
        const averageRating = data.averageRating || 0;
        const totalRatings = data.totalRatings || 0;
        
        // Update average display
        if (totalRatings > 0) {
            avgDisplay.textContent = `★ ${(averageRating / 2).toFixed(1)} (${totalRatings} ${totalRatings === 1 ? 'rating' : 'ratings'})`;
        } else {
            avgDisplay.textContent = 'No ratings yet';
        }
        
        if (ratings.length === 0) {
            return;
        }
        
        const title = document.createElement('div');
        title.className = 'user-ratings-section-title';
        title.textContent = 'All Ratings';
        allRatingsSection.appendChild(title);
        
        ratings.forEach(rating => {
            const item = document.createElement('div');
            item.className = 'rating-item';
            
            // Header with user, stars, and date
            const header = document.createElement('div');
            header.className = 'rating-item-header';
            
            const leftSide = document.createElement('div');
            const userName = document.createElement('span');
            userName.className = 'rating-item-user';
            userName.textContent = rating.userName || rating.UserName || 'Unknown User';
            leftSide.appendChild(userName);
            
    const stars = document.createElement('span');
    stars.className = 'rating-item-stars';
    const ratingValue = rating.rating || rating.Rating || 0;
    var fullStars = Math.floor(ratingValue / 2);
    var hasHalf = ratingValue % 2 === 1;
    var emptyStars = 5 - fullStars - (hasHalf ? 1 : 0);
    stars.textContent = '\u2605'.repeat(fullStars) + (hasHalf ? '\u00BD' : '') + '\u2606'.repeat(emptyStars);
            leftSide.appendChild(stars);
            
            header.appendChild(leftSide);
            
            // Date
            const timestamp = rating.timestamp || rating.Timestamp;
            if (timestamp) {
                const date = document.createElement('span');
                date.className = 'rating-item-date';
                const dateObj = new Date(timestamp);
                date.textContent = dateObj.toLocaleDateString(undefined, { 
                    year: 'numeric', 
                    month: 'short', 
                    day: 'numeric' 
                });
                header.appendChild(date);
            }
            
            item.appendChild(header);
            
            // Note
            const noteText = rating.note || rating.Note;
            if (noteText) {
                const note = document.createElement('div');
                note.className = 'rating-item-note';
                note.textContent = noteText;
                item.appendChild(note);
            }
            
            allRatingsSection.appendChild(item);
        });
        console.log('[UserRatings] → displayAllRatings completed');
    }

    let injectionAttempts = 0;
    const maxInjectionAttempts = 30; // Increased from 20
    
    function injectRatingsUI() {
        // Prevent concurrent injections
        if (isInjecting) {
            console.log('[UserRatings] Already injecting, skipping');
            return;
        }
        
        // Get item ID from URL first
        let itemId = null;
        const urlParams = new URLSearchParams(window.location.search);
        itemId = urlParams.get('id');
        
        if (!itemId && window.location.hash.includes('?')) {
            const hashParams = new URLSearchParams(window.location.hash.split('?')[1]);
            itemId = hashParams.get('id');
        }
        
        if (!itemId) {
            const guidMatch = window.location.href.match(/([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})/i);
            if (guidMatch) {
                itemId = guidMatch[1];
            }
        }
        
        if (!itemId) {
            console.log('[UserRatings] No item ID found');
            injectionAttempts = 0;
            return;
        }
        
        // Check if UI already exists - if for same item, skip; if different item, remove it
        const existingUI = document.getElementById('user-ratings-ui');
        if (existingUI && currentItemId === itemId) {
            console.log('[UserRatings] UI already exists for this item, skipping');
            injectionAttempts = 0;
            return;
        }
        
        // If UI exists for different item, remove it
        if (existingUI && currentItemId !== itemId) {
            console.log('[UserRatings] Removing UI for previous item');
            existingUI.remove();
        }
        
        // Try multiple selector strategies to find the container
        let targetContainer = null;
        
        // Strategy 1: Look for .detailSection inside .detailPagePrimaryContent
        targetContainer = document.querySelector('.detailPagePrimaryContent .detailSection');
        
        // Strategy 2: Look for .detailPagePrimaryContent itself
        if (!targetContainer) {
            const primaryContent = document.querySelector('.detailPagePrimaryContent');
            if (primaryContent) {
                // Check if it has children (content loaded)
                if (primaryContent.children.length > 0) {
                    targetContainer = primaryContent;
                }
            }
        }
        
        // Strategy 3: Look for any detail section
        if (!targetContainer) {
            targetContainer = document.querySelector('.detailSection');
        }
        
        // Strategy 4: Look for itemDetailPage
        if (!targetContainer) {
            const detailPage = document.querySelector('.itemDetailPage .detailPageContent');
            if (detailPage) {
                targetContainer = detailPage;
            }
        }
        
        if (!targetContainer) {
            if (injectionAttempts < maxInjectionAttempts) {
                injectionAttempts++;
                const retryDelay = Math.min(100 * Math.pow(1.5, injectionAttempts), 3000);
                console.log(`[UserRatings] Container not ready, retry ${injectionAttempts}/${maxInjectionAttempts} in ${retryDelay.toFixed(0)}ms`);
                setTimeout(injectRatingsUI, retryDelay);
            } else {
                console.log('[UserRatings] Max injection attempts reached, resetting for retry');
                injectionAttempts = 0;
                isInjecting = false;
            }
            return;
        }
        
        currentItemId = itemId;
        isInjecting = true;
        injectionAttempts = 0; // Reset counter on successful injection
        console.log('[UserRatings] Injecting UI for item:', itemId, 'into container:', targetContainer.className);
        
        // Create and inject UI at the end of target container
        createRatingsUI(itemId).then(ui => {
            targetContainer.appendChild(ui);
            
            // Mark injection complete after a short delay to allow rendering
            setTimeout(() => {
                isInjecting = false;
                console.log('[UserRatings] ✓ UI injected successfully');
            }, 200);
            
        }).catch(err => {
            console.error('[UserRatings] Error creating UI:', err);
            isInjecting = false;
            injectionAttempts = 0;
        });
    }
    
    // Watch for page changes with more aggressive detection
    let lastUrl = location.href;
    let lastCheckedItemId = null;
    new MutationObserver((mutations) => {
        const url = location.href;
        
        // Check if URL changed
        if (url !== lastUrl) {
            lastUrl = url;
            
            // Remove old UI when navigating to a new page
            const oldUI = document.getElementById('user-ratings-ui');
            if (oldUI) {
                console.log('[UserRatings] Removing old UI on navigation');
                oldUI.remove();
            }
            
            // Hide ratings tab when navigating away from home
            const ratingsTab = document.querySelector('#ratingsTab');
            if (ratingsTab && !url.includes('#/home')) {
                ratingsTab.classList.remove('is-active');
                ratingsTab.style.display = 'none';
            }
            
            // Reset injection state (don't mark as navigating - let normal navigation proceed)
            isInjecting = false;
            injectionAttempts = 0;
            currentItemId = null;
            
            // Try injection with slight delay
            setTimeout(injectRatingsUI, 150);
            return;
        }
        
        // Even if URL didn't change, check if detail page content appeared
        // This handles cases where the page loads but URL was already set
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node.nodeType === 1) { // Element node
                    // Check if a detail page container was added
                    if (node.classList && (
                        node.classList.contains('detailPagePrimaryContent') ||
                        node.classList.contains('detailSection') ||
                        node.classList.contains('itemDetailPage')
                    )) {
                        console.log('[UserRatings] Detail page container detected, triggering injection');
                        isInjecting = false;
                        injectionAttempts = 0;
                        setTimeout(injectRatingsUI, 100);
                        return;
                    }
                    // Also check children
                    if (node.querySelector && (
                        node.querySelector('.detailPagePrimaryContent') ||
                        node.querySelector('.detailSection') ||
                        node.querySelector('.itemDetailPage')
                    )) {
                        console.log('[UserRatings] Detail page content detected in mutation, triggering injection');
                        isInjecting = false;
                        injectionAttempts = 0;
                        setTimeout(injectRatingsUI, 100);
                        return;
                    }
                }
            }
        }
    }).observe(document.body, { subtree: true, childList: true, attributes: true, characterData: true });

    // Initial injection - start with multiple attempts at different intervals
    setTimeout(injectRatingsUI, 100);
    setTimeout(injectRatingsUI, 300);
    setTimeout(injectRatingsUI, 600);
    
    // Also check on hash change
    window.addEventListener('hashchange', () => {
        // Remove old UI on hash change
        const oldUI = document.getElementById('user-ratings-ui');
        if (oldUI) {
            oldUI.remove();
        }
        
        // Manage page visibility
        const ratingsTab = document.querySelector('#ratingsTab');
        const currentHash = window.location.hash;
        
        if (ratingsTab) {
            if (!currentHash.includes('home')) {
                // Navigating away from home - hide ratings page
                console.log('[UserRatings] Navigating away from home - hiding ratings page');
                ratingsTab.style.display = 'none';
                ratingsTab.classList.add('hide');
            } else if (currentHash.includes('home')) {
                // Back to home - check whether User Ratings was the active tab
                const wasUserRatings = window.history.state && window.history.state.userRatingsActive;

                if (wasUserRatings) {
                    // Back from details -> restore User Ratings tab
                    console.log('[UserRatings] Restoring User Ratings tab (history.state.userRatingsActive)');
                    const homePage = document.querySelector('[data-role="page"].homePage:not(#ratingsTab)');
                    if (homePage) homePage.classList.add('hide');
                    ratingsTab.classList.remove('hide');
                    ratingsTab.style.display = 'block';

                    // Re-mark the User Ratings tab button as active
                    const tabBtn = document.querySelector('[data-ratings-tab="true"]');
                    if (tabBtn) {
                        document.querySelectorAll('.emby-tab-button').forEach(t => t.classList.remove('emby-tab-button-active'));
                        tabBtn.classList.add('emby-tab-button-active');
                    }

                    // Re-fetch if there were rating changes since last visit
                    const hasChanges = sessionStorage.getItem('userRatingsDirty') === 'true';
                    if (hasChanges) {
                        sessionStorage.removeItem('userRatingsDirty');
                        console.log('[UserRatings] Pending rating changes detected, re-fetching data');
                        displayRatingsList().then(() => restoreLastSection());
                    }
                } else {
                    // Native home navigation - hide ratings, show home
                    console.log('[UserRatings] Navigating to home - ensuring clean state');
                    ratingsTab.style.display = 'none';
                    ratingsTab.classList.add('hide');

                    // Hide ALL pages except home
                    const allPages = document.querySelectorAll('[data-role="page"]');
                    allPages.forEach(page => {
                        if (page.id === 'ratingsTab' || !page.classList.contains('homePage')) {
                            page.classList.add('hide');
                            page.style.display = 'none';
                        }
                    });

                    // Show only the home page
                    const homePage = document.querySelector('[data-role="page"].homePage:not(#ratingsTab)');
                    if (homePage) {
                        homePage.classList.remove('hide');
                        homePage.style.display = '';
                        console.log('[UserRatings] Restored home page only');
                    }
                }
            }
        }
        
        // Reset injection state (don't mark as navigating - let normal navigation proceed)
        isInjecting = false;
        injectionAttempts = 0;
        currentItemId = null;
        
        // Try injection with multiple attempts
        setTimeout(injectRatingsUI, 100);
        setTimeout(injectRatingsUI, 300);
    });

    // Also check on popstate (browser back/forward, pushState navigation)
    window.addEventListener('popstate', () => {
        const oldUI = document.getElementById('user-ratings-ui');
        if (oldUI) {
            oldUI.remove();
        }

        isInjecting = false;
        injectionAttempts = 0;
        currentItemId = null;

        setTimeout(injectRatingsUI, 100);
        setTimeout(injectRatingsUI, 300);
    });

    // Anchor-scroll to the section the user was last viewing (if section still exists)
    function restoreLastSection() {
        const title = window.history.state && window.history.state.lastVisibleSection;
        if (!title) return;

        const start = Date.now();
        const poll = () => {
            const h2 = Array.from(document.querySelectorAll('#ratingsTab h2.sectionTitle'))
                .find(h => h.textContent.trim() === title);
            if (h2) {
                h2.scrollIntoView({ block: 'start', behavior: 'instant' });
            } else if (Date.now() - start < 8000) {
                requestAnimationFrame(poll);
            }
        };
        poll();
    }

    // Track which section the user clicks a card in, so we can return there on back-navigation
    (function initSectionTracker() {
        document.addEventListener('click', function urClickTracker(e) {
            const link = e.target.closest('a[href*="details"]');
            if (!link) return;
            const ratingsTab = document.getElementById('ratingsTab');
            if (!ratingsTab || !ratingsTab.contains(link)) return;
            const section = link.closest('.verticalSection');
            if (!section) return;
            const titleEl = section.querySelector('h2.sectionTitle');
            if (!titleEl) return;
            try {
                const state = window.history.state || {};
                state.lastVisibleSection = titleEl.textContent.trim();
                window.history.replaceState(state, '', window.location.href);
            } catch (err) {}
        }, true);
    })();

    // Function to display ratings list in the home page content area
    async function displayRatingsList() {
        // Find or create the ratings tab content container
        let ratingsTabContent = document.querySelector('#ratingsTab');
        
            if (!ratingsTabContent) {
                // Find the home page - this is the main page container
                const homePage = document.querySelector('[data-role="page"]:not(.hide)');
                
                if (!homePage) {
                    console.error('[UserRatings] Could not find home page');
                    return;
                }
                
                // Try multiple selectors to find the content container
                let scrollContainer = homePage.querySelector('.scrollY');
                if (!scrollContainer) {
                    scrollContainer = homePage.querySelector('.pageTabContent');
                }
                if (!scrollContainer) {
                    scrollContainer = homePage.querySelector('.scrollContainer');
                }
                if (!scrollContainer) {
                    // Just use the page itself as the container
                    scrollContainer = homePage;
                }
                
                ratingsTabContent = document.createElement('div');
                ratingsTabContent.id = 'ratingsTab';
                ratingsTabContent.className = 'page homePage libraryPage hide';
                ratingsTabContent.setAttribute('data-role', 'page');
                ratingsTabContent.style.position = 'absolute';
                ratingsTabContent.style.top = '0';
                ratingsTabContent.style.left = '0';
                ratingsTabContent.style.right = '0';
                ratingsTabContent.style.bottom = '0';
                ratingsTabContent.style.overflow = 'auto';
                
                // Add as sibling to home page
                homePage.parentNode.appendChild(ratingsTabContent);
            }
        
        // Hide the home page and show ratings tab
        const homePage = document.querySelector('[data-role="page"]:not(.hide):not(#ratingsTab)');
        if (homePage) {
            homePage.classList.add('hide');
        }
        
        ratingsTabContent.classList.remove('hide');
        ratingsTabContent.style.display = 'block';
        ratingsTabContent.style.pointerEvents = 'auto';

        // Show loading
        ratingsTabContent.innerHTML = '<div style="padding: 3em 2em; text-align: center; color: rgba(255,255,255,0.6);">Loading ratings...</div>';

        try {
            // Fetch user's ratings for compact badge display
            await fetchUserRatings();

            // Get config for page size (items per page in Rated Movies/Shows + Watched sections)
            let perPage = 24;
            try {
                const pluginConfig = await ApiClient.getPluginConfiguration('b8c5d3e7-4f6a-8b9c-1d2e-3f4a5b6c7d8e');
                perPage = pluginConfig.RecentlyRatedItemsCount || 10;
            } catch (error) {}

            function getItemCardImage(itemId, seriesId, itemType) {
                const imageId = itemType === 'Episode' && seriesId ? seriesId : itemId;
                return {
                    thumb: ApiClient.getImageUrl(imageId, { type: 'Thumb', fillWidth: 426, fillHeight: 240, quality: 80 }),
                    backdrop: ApiClient.getImageUrl(imageId, { type: 'Backdrop', fillWidth: 426, fillHeight: 240, quality: 80 }),
                    primary: ApiClient.getImageUrl(imageId, { type: 'Primary', fillWidth: 426, fillHeight: 240, quality: 80 })
                };
            }

            function applyImageFallback(el) {
                const thumb = el.getAttribute('data-thumb');
                const backdrop = el.getAttribute('data-backdrop');
                const primary = el.getAttribute('data-primary');
                const step = parseInt(el.getAttribute('data-fallback-step') || '0');
                const urls = [thumb, backdrop, primary];
                if (step < urls.length) {
                    const testImg = new Image();
                    testImg.onload = function() {
                        el.style.backgroundImage = "url('" + urls[step] + "')";
                    };
                    testImg.onerror = function() {
                        el.setAttribute('data-fallback-step', String(step + 1));
                        applyImageFallback(el);
                    };
                    testImg.src = urls[step];
                } else {
                    el.style.backgroundImage = 'none';
                }
            }

            // Function to build the ratings grid HTML for a category (backdrop cards in vertical-wrap)
            const buildCategoryGrid = (items) => items.map(item => {
                const details = item.details || item;
                const itemId = item.itemId || details.Id || details.itemId;
                const seriesId = details.SeriesId || details.seriesId;
                const itemType = details.Type || details.type;
                const urls = getItemCardImage(itemId, seriesId, itemType);
                const title = details.Name || details.name || 'Unknown';
                const rating = (item.averageRating / 2).toFixed(1);
                const count = item.totalRatings;
                const serverId = ApiClient.serverId();

                return `
                    <div data-index="0" data-isfolder="false" data-serverid="${serverId}" data-id="${itemId}" data-type="${itemType}" data-mediatype="Video" class="card backdropCard card-hoverable card-withuserdata">
                        <div class="cardBox cardBox-bottompadded">
                            <div class="cardScalable">
                                <div class="cardPadder cardPadder-backdrop"></div>
                                <a href="#/details?id=${itemId}&serverId=${serverId}" data-action="link" class="cardImageContainer cardContent itemAction" aria-label="${title}" data-thumb="${urls.thumb}" data-backdrop="${urls.backdrop}" data-primary="${urls.primary}" data-fallback-step="0"></a>
                                <div class="compact-rating" data-empty="true" data-item-id="${itemId}" style="display:none;">
                                    <span class="cr-star">&#x2605;</span>
                                    <span class="cr-value"></span>
                                    <span class="cr-edit">&#x270E;</span>
                                </div>
                                <div class="cardIndicators cardIndicators-bottomright">
                                    <div style="background: rgba(0,0,0,0.85); padding: 0.4em 0.7em; border-radius: 4px; display: inline-flex; align-items: center; gap: 0.3em;">
                                        <span style="color: #ffd700; font-size: 1.1em;">★</span>
                                        <span style="font-weight: 600;">${rating}</span>
                                        <span style="opacity: 0.7; font-size: 0.85em;">(${count})</span>
                                    </div>
                                </div>
                            </div>
                            <div class="cardText cardTextCentered cardText-first">
                                <bdi>
                                    <a href="#/details?id=${itemId}&serverId=${serverId}" data-id="${itemId}" data-serverid="${serverId}" data-type="${itemType}" data-action="link" class="itemAction textActionButton" title="${title}">${title}</a>
                                </bdi>
                            </div>
                            <div class="cardText cardTextCentered">&nbsp;</div>
                        </div>
                    </div>
                `;
            }).join('');

            // Function to build unrated item cards (backdrop cards with "Unrated" badge)
            const buildUnratedGrid = (items) => items.map(item => {
                const serverId = ApiClient.serverId();
                const urls = getItemCardImage(item.itemId, item.seriesId, item.type);
                const title = item.name || 'Unknown';

                return `
                    <div data-index="0" data-isfolder="false" data-serverid="${serverId}" data-id="${item.itemId}" data-type="${item.type}" data-mediatype="Video" class="card backdropCard card-hoverable card-withuserdata">
                        <div class="cardBox cardBox-bottompadded">
                            <div class="cardScalable">
                                <div class="cardPadder cardPadder-backdrop"></div>
                                <div class="compact-rating" data-empty="true" data-item-id="${item.itemId}" style="display:none;">
                                    <span class="cr-star">&#x2605;</span>
                                    <span class="cr-value"></span>
                                    <span class="cr-edit">&#x270E;</span>
                                </div>
                                <a href="#/details?id=${item.itemId}&serverId=${serverId}" data-action="link" class="cardImageContainer cardContent itemAction" aria-label="${title}" data-thumb="${urls.thumb}" data-backdrop="${urls.backdrop}" data-primary="${urls.primary}" data-fallback-step="0"></a>
                                <div class="cardIndicators cardIndicators-bottomright">
                                    <div class="rate-badge" data-item-id="${item.itemId}">
                                        <span class="material-icons" style="font-size: 0.9em;">star_border</span>
                                        <span>Rate</span>
                                    </div>
                                </div>
                            </div>
                            <div class="cardText cardTextCentered cardText-first">
                                <bdi>
                                    <a href="#/details?id=${item.itemId}&serverId=${serverId}" data-id="${item.itemId}" data-serverid="${serverId}" data-type="${item.type}" data-action="link" class="itemAction textActionButton" title="${title}">${title}</a>
                                </bdi>
                            </div>
                            <div class="cardText cardTextCentered">&nbsp;</div>
                        </div>
                    </div>
                `;
            }).join('');

            const accessToken = ApiClient.accessToken();

            // Fetch a page of rated items from the server (server-side pagination)
            async function fetchRatedItemsPage(offset, limit, sortBy, sortDir, typeFilter) {
                const params = new URLSearchParams({ offset, limit, sortBy, sortDir });
                if (typeFilter && typeFilter !== 'all') params.set('typeFilter', typeFilter);
                const url = ApiClient.getUrl('api/UserRatings/AllRatedItems') + '?' + params.toString();
                const resp = await fetch(url, { headers: { 'X-Emby-Token': accessToken } });
                if (!resp.ok) return { items: [], total: 0 };
                const data = await resp.json();
                return { items: data.items || [], total: data.total || 0 };
            }

            // Normalize server item to the shape expected by buildCategoryGrid
            function normalizeRatedItem(item) {
                return {
                    ...item,
                    lastRatedTimestamp: item.lastRated ? new Date(item.lastRated).getTime() : 0,
                    details: {
                        Name: item.name,
                        Type: item.type,
                        SeriesId: item.seriesId,
                        Id: item.itemId
                    }
                };
            }

            // ===== Movies section state =====
            let movies_currentPage = 1;
            let movies_sortField = 'recent';
            let movies_sortDir = 'desc';
            let movies_total = 0;

            // ===== Shows section state =====
            let shows_currentPage = 1;
            let shows_sortField = 'recent';
            let shows_sortDir = 'desc';
            let shows_typeFilter = 'Series,Episode';
            let shows_total = 0;

            // Build sections HTML — Movies + Shows placeholders, then unrated placeholders
            let sectionsHTML = '<div style="padding-top: 4em;">';
            sectionsHTML += '<div id="moviesSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);"><span class="material-icons" style="vertical-align: middle; margin-right: 0.3em;">hourglass_empty</span>Loading rated movies...</div></div>';
            sectionsHTML += '<div id="showsSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);"><span class="material-icons" style="vertical-align: middle; margin-right: 0.3em;">hourglass_empty</span>Loading rated shows...</div></div>';
            sectionsHTML += '</div>';
            sectionsHTML += '<div id="unratedMoviesSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">Loading watched movies...</div></div>';
            sectionsHTML += '<div id="unratedSeriesSection"><div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);"><span class="material-icons" style="vertical-align: middle; margin-right: 0.3em;">hourglass_empty</span>Loading watched shows...<br><span style="font-size: 0.85em; opacity: 0.7;">This may take a few seconds</span></div></div>';

            // Display the page immediately
            ratingsTabContent.innerHTML = sectionsHTML;
            ratingsTabContent.style.pointerEvents = 'auto';

            // Initialize image fallbacks for all cards
            ratingsTabContent.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);

            // ===== Render Movies section (server-side paginated, sort + direction) =====
            const moviesSection = document.querySelector('#moviesSection');

            async function renderMoviesSection(page) {
                const offset = (page - 1) * perPage;
                const { items, total } = await fetchRatedItemsPage(
                    offset, perPage, movies_sortField, movies_sortDir, 'Movie'
                );
                movies_total = total;
                const pageItems = items.map(normalizeRatedItem);

                const totalPages = Math.max(1, Math.ceil(total / perPage));
                const startIndex = offset + 1;
                const endIndex = Math.min(offset + perPage, total);
                const dirArrow = movies_sortDir === 'desc' ? 'arrow_downward' : 'arrow_upward';

                moviesSection.innerHTML = `
                    <div class="verticalSection">
                        <div class="sectionTitleContainer sectionTitleContainer-cards padded-left">
                            <h2 class="sectionTitle sectionTitle-cards">Rated Movies</h2>
                        </div>
                        <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x" style="gap: 1em;">
                            <div class="paging">
                                <div class="listPaging">
                                    <span style="vertical-align:middle;">${total > 0 ? startIndex + '-' + endIndex : 0} of ${total}</span>
                                    <div style="display:inline-block;">
                                        <button is="paper-icon-button-light" class="prevPageBtn autoSize paper-icon-button-light" ${page === 1 ? 'disabled' : ''}>
                                            <span class="material-icons arrow_back" aria-hidden="true"></span>
                                        </button>
                                        <button is="paper-icon-button-light" class="nextPageBtn autoSize paper-icon-button-light" ${page >= totalPages ? 'disabled' : ''}>
                                            <span class="material-icons arrow_forward" aria-hidden="true"></span>
                                        </button>
                                    </div>
                                </div>
                            </div>
                            <select is="emby-select" class="sortSelect emby-select-withcolor emby-select" style="width: auto;">
                                <option value="rating" ${movies_sortField === 'rating' ? 'selected' : ''}>Rating</option>
                                <option value="title" ${movies_sortField === 'title' ? 'selected' : ''}>Title</option>
                                <option value="recent" ${movies_sortField === 'recent' ? 'selected' : ''}>Recently Rated</option>
                                <option value="count" ${movies_sortField === 'count' ? 'selected' : ''}>Most Ratings</option>
                            </select>
                            <button is="paper-icon-button-light" class="sortDirBtn autoSize paper-icon-button-light" title="Toggle sort direction">
                                <span class="material-icons ${dirArrow}" aria-hidden="true"></span>
                            </button>
                        </div>
                        <div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">
                            ${pageItems.length > 0 ? buildCategoryGrid(pageItems) : '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">No rated movies found.</div>'}
                        </div>
                    </div>
                `;

                moviesSection.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);
                fillCompactBadges();
                attachRatedCardListeners(moviesSection);

                const sortSelect = moviesSection.querySelector('.sortSelect');
                if (sortSelect) {
                    sortSelect.addEventListener('change', (e) => {
                        movies_sortField = e.target.value;
                        movies_currentPage = 1;
                        renderMoviesSection(movies_currentPage);
                    });
                }

                const sortDirBtn = moviesSection.querySelector('.sortDirBtn');
                if (sortDirBtn) {
                    sortDirBtn.addEventListener('click', () => {
                        movies_sortDir = movies_sortDir === 'desc' ? 'asc' : 'desc';
                        movies_currentPage = 1;
                        renderMoviesSection(movies_currentPage);
                    });
                }

                const prevBtn = moviesSection.querySelector('.prevPageBtn');
                if (prevBtn && !prevBtn.disabled) {
                    prevBtn.addEventListener('click', () => {
                        movies_currentPage--;
                        renderMoviesSection(movies_currentPage);
                    });
                }

                const nextBtn = moviesSection.querySelector('.nextPageBtn');
                if (nextBtn && !nextBtn.disabled) {
                    nextBtn.addEventListener('click', () => {
                        movies_currentPage++;
                        renderMoviesSection(movies_currentPage);
                    });
                }
            }

            // ===== Render Shows section (server-side paginated, sort + direction + tab sub-filter) =====
            const showsSection = document.querySelector('#showsSection');

            async function renderShowsSection(page) {
                const offset = (page - 1) * perPage;
                const { items, total } = await fetchRatedItemsPage(
                    offset, perPage, shows_sortField, shows_sortDir, shows_typeFilter
                );
                shows_total = total;
                const pageItems = items.map(normalizeRatedItem);

                const totalPages = Math.max(1, Math.ceil(total / perPage));
                const startIndex = offset + 1;
                const endIndex = Math.min(offset + perPage, total);
                const dirArrow = shows_sortDir === 'desc' ? 'arrow_downward' : 'arrow_upward';

                // Tab sub-filter: All (Series+Episode) / Shows (Series) / Episodes (Episode)
                const tabAll = shows_typeFilter === 'Series,Episode';
                const tabShows = shows_typeFilter === 'Series';
                const tabEpisodes = shows_typeFilter === 'Episode';

                const typeTabsHtml = `
                    <div class="showsTypeTabs" style="display:inline-flex;gap:0;">
                        <button is="emby-button" class="typeTabBtn emby-button ${tabAll ? 'typeTabActive' : ''}" data-type="all" style="padding:0.5em 1em;font-size:0.85em;border-radius:4px 0 0 4px;background:${tabAll ? 'rgba(255,255,255,0.15)' : 'transparent'};color:${tabAll ? '#fff' : 'rgba(255,255,255,0.5)'};border:none;cursor:pointer;">All</button>
                        <button is="emby-button" class="typeTabBtn emby-button ${tabShows ? 'typeTabActive' : ''}" data-type="Series" style="padding:0.5em 1em;font-size:0.85em;border-radius:0;background:${tabShows ? 'rgba(255,255,255,0.15)' : 'transparent'};color:${tabShows ? '#fff' : 'rgba(255,255,255,0.5)'};border:none;border-left:1px solid rgba(255,255,255,0.1);cursor:pointer;">Shows</button>
                        <button is="emby-button" class="typeTabBtn emby-button ${tabEpisodes ? 'typeTabActive' : ''}" data-type="Episode" style="padding:0.5em 1em;font-size:0.85em;border-radius:0 4px 4px 0;background:${tabEpisodes ? 'rgba(255,255,255,0.15)' : 'transparent'};color:${tabEpisodes ? '#fff' : 'rgba(255,255,255,0.5)'};border:none;border-left:1px solid rgba(255,255,255,0.1);cursor:pointer;">Episodes</button>
                    </div>`;

                showsSection.innerHTML = `
                    <div class="verticalSection">
                        <div class="sectionTitleContainer sectionTitleContainer-cards padded-left">
                            <h2 class="sectionTitle sectionTitle-cards">Rated Shows</h2>
                        </div>
                        <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x" style="gap: 1em;">
                            <div class="paging">
                                <div class="listPaging">
                                    <span style="vertical-align:middle;">${total > 0 ? startIndex + '-' + endIndex : 0} of ${total}</span>
                                    <div style="display:inline-block;">
                                        <button is="paper-icon-button-light" class="prevPageBtn autoSize paper-icon-button-light" ${page === 1 ? 'disabled' : ''}>
                                            <span class="material-icons arrow_back" aria-hidden="true"></span>
                                        </button>
                                        <button is="paper-icon-button-light" class="nextPageBtn autoSize paper-icon-button-light" ${page >= totalPages ? 'disabled' : ''}>
                                            <span class="material-icons arrow_forward" aria-hidden="true"></span>
                                        </button>
                                    </div>
                                </div>
                            </div>
                            ${typeTabsHtml}
                            <select is="emby-select" class="sortSelect emby-select-withcolor emby-select" style="width: auto;">
                                <option value="rating" ${shows_sortField === 'rating' ? 'selected' : ''}>Rating</option>
                                <option value="title" ${shows_sortField === 'title' ? 'selected' : ''}>Title</option>
                                <option value="recent" ${shows_sortField === 'recent' ? 'selected' : ''}>Recently Rated</option>
                                <option value="count" ${shows_sortField === 'count' ? 'selected' : ''}>Most Ratings</option>
                            </select>
                            <button is="paper-icon-button-light" class="sortDirBtn autoSize paper-icon-button-light" title="Toggle sort direction">
                                <span class="material-icons ${dirArrow}" aria-hidden="true"></span>
                            </button>
                        </div>
                        <div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">
                            ${pageItems.length > 0 ? buildCategoryGrid(pageItems) : '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">No rated shows found.</div>'}
                        </div>
                    </div>
                `;

                showsSection.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);
                fillCompactBadges();
                attachRatedCardListeners(showsSection);

                const sortSelect = showsSection.querySelector('.sortSelect');
                if (sortSelect) {
                    sortSelect.addEventListener('change', (e) => {
                        shows_sortField = e.target.value;
                        shows_currentPage = 1;
                        renderShowsSection(shows_currentPage);
                    });
                }

                const sortDirBtn = showsSection.querySelector('.sortDirBtn');
                if (sortDirBtn) {
                    sortDirBtn.addEventListener('click', () => {
                        shows_sortDir = shows_sortDir === 'desc' ? 'asc' : 'desc';
                        shows_currentPage = 1;
                        renderShowsSection(shows_currentPage);
                    });
                }

                const typeTabBtns = showsSection.querySelectorAll('.typeTabBtn');
                typeTabBtns.forEach(function(btn) {
                    btn.addEventListener('click', function(e) {
                        e.preventDefault();
                        const t = this.getAttribute('data-type');
                        if (t === 'all') shows_typeFilter = 'Series,Episode';
                        else if (t === 'Series') shows_typeFilter = 'Series';
                        else if (t === 'Episode') shows_typeFilter = 'Episode';
                        shows_currentPage = 1;
                        renderShowsSection(shows_currentPage);
                    });
                });

                const prevBtn = showsSection.querySelector('.prevPageBtn');
                if (prevBtn && !prevBtn.disabled) {
                    prevBtn.addEventListener('click', () => {
                        shows_currentPage--;
                        renderShowsSection(shows_currentPage);
                    });
                }

                const nextBtn = showsSection.querySelector('.nextPageBtn');
                if (nextBtn && !nextBtn.disabled) {
                    nextBtn.addEventListener('click', () => {
                        shows_currentPage++;
                        renderShowsSection(shows_currentPage);
                    });
                }
            }

            // Render Movies + Shows sections (async, non-blocking)
            renderMoviesSection(movies_currentPage).catch(e => {
                console.error('[UserRatings] Error loading movies section:', e);
                if (moviesSection) moviesSection.innerHTML = '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">Error loading rated movies.</div>';
            });
            renderShowsSection(shows_currentPage).catch(e => {
                console.error('[UserRatings] Error loading shows section:', e);
                if (showsSection) showsSection.innerHTML = '<div style="padding: 2em; text-align: center; color: rgba(255,255,255,0.5);">Error loading rated shows.</div>';
            });

            // Fetch unrated items via server-side endpoint
            const userId = ApiClient.getCurrentUserId();

            async function fetchUnratedType(itemType) {
                try {
                    const url = ApiClient.getUrl(`api/UserRatings/UnratedWatchedItems?userId=${userId}&itemType=${itemType}`);
                    const resp = await fetch(url, { headers: { 'X-Emby-Token': accessToken } });
                    if (!resp.ok) return [];
                    const data = await resp.json();
                    if (!data.items) return [];
                    return data.items.slice(0, 24);
                } catch (e) {
                    console.error('[UserRatings] Error fetching unrated items:', e);
                    return [];
                }
            }

            // Fetch unrated sections. Movies (~30ms) arrive fast, Series (~5s) is slow.
            Promise.all([
                fetchUnratedType('Movie'),
                fetchUnratedType('Series')
            ]).then(([unratedMoviesList, unratedSeriesList]) => {
                const unratedMoviesSection = document.querySelector('#unratedMoviesSection');
                if (unratedMoviesSection) {
                    if (unratedMoviesList.length > 0) {
                        const mapped = unratedMoviesList.map((item, idx) => ({
                            itemId: item.itemId,
                            name: item.name,
                            type: 'Movie',
                            averageRating: 0,
                            totalRatings: 0,
                            lastPlayedDate: item.lastPlayedDate || null,
                            _sortOrder: idx
                        }));
                        renderUnratedSection(unratedMoviesSection, mapped, 'Watched Movies — Not Yet Rated');
                    } else {
                        unratedMoviesSection.innerHTML = '';
                    }
                }

                const unratedSeriesSection = document.querySelector('#unratedSeriesSection');
                if (unratedSeriesSection) {
                    if (unratedSeriesList.length > 0) {
                        const mapped = unratedSeriesList.map((item, idx) => ({
                            itemId: item.itemId,
                            name: item.name,
                            type: 'Series',
                            averageRating: 0,
                            totalRatings: 0,
                            lastPlayedDate: item.lastPlayedDate || null,
                            _sortOrder: idx
                        }));
                        renderUnratedSection(unratedSeriesSection, mapped, 'Watched Shows — Not Yet Rated');
                    } else {
                        unratedSeriesSection.innerHTML = '';
                    }
                }
            }).then(() => {
                fillCompactBadges();
                attachRateButtonListeners(document.querySelector('#ratingsTab'));
                attachRatedCardListeners(document.querySelector('#ratingsTab'));
            });

            // Helper: render an unrated section with client-side pagination (small fixed set, no server call)
            function renderUnratedSection(container, items, title) {
                let unratedPage = 1;
                let unratedSortDir = 'desc';
                let unratedSortField = 'watched';

                const sortUnrated = (arr, field, dir) => {
                    const d = dir === 'asc' ? 1 : -1;
                    if (field === 'watched') {
                        arr.sort((a, b) => {
                            if (a.lastPlayedDate && b.lastPlayedDate) return d === -1
                                ? b.lastPlayedDate.localeCompare(a.lastPlayedDate)
                                : a.lastPlayedDate.localeCompare(b.lastPlayedDate);
                            if (a.lastPlayedDate) return -1;
                            if (b.lastPlayedDate) return 1;
                            return (a._sortOrder || 0) - (b._sortOrder || 0);
                        });
                    } else if (field === 'title') {
                        arr.sort((a, b) => (a.name || '').localeCompare(b.name || '') * d);
                    }
                };

                sortUnrated(items, unratedSortField, unratedSortDir);

                function renderPage(page) {
                    const totalPages = Math.max(1, Math.ceil(items.length / perPage));
                    const startIndex = (page - 1) * perPage;
                    const endIndex = Math.min(startIndex + perPage, items.length);
                    const paginatedItems = items.slice(startIndex, endIndex);
                    const dirArrow = unratedSortDir === 'desc' ? 'arrow_downward' : 'arrow_upward';

                    container.innerHTML = `
                        <div class="verticalSection">
                            <div class="sectionTitleContainer sectionTitleContainer-cards padded-left">
                                <h2 class="sectionTitle sectionTitle-cards">${title}</h2>
                            </div>
                            <div class="flex align-items-center justify-content-center flex-wrap-wrap padded-top padded-left padded-right padded-bottom focuscontainer-x" style="gap: 1em;">
                                <div class="paging">
                                    <div class="listPaging">
                                        <span style="vertical-align:middle;">${items.length > 0 ? (startIndex + 1) + '-' + endIndex : 0} of ${items.length}</span>
                                        <div style="display:inline-block;">
                                            <button is="paper-icon-button-light" class="prevPageBtn autoSize paper-icon-button-light" ${page === 1 ? 'disabled' : ''}>
                                                <span class="material-icons arrow_back" aria-hidden="true"></span>
                                            </button>
                                            <button is="paper-icon-button-light" class="nextPageBtn autoSize paper-icon-button-light" ${page >= totalPages ? 'disabled' : ''}>
                                                <span class="material-icons arrow_forward" aria-hidden="true"></span>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                                <select is="emby-select" class="sortSelect emby-select-withcolor emby-select" style="width: auto;">
                                    <option value="watched" ${unratedSortField === 'watched' ? 'selected' : ''}>Last Watched</option>
                                    <option value="title" ${unratedSortField === 'title' ? 'selected' : ''}>Title</option>
                                </select>
                                <button is="paper-icon-button-light" class="sortDirBtn autoSize paper-icon-button-light" title="Toggle sort direction">
                                    <span class="material-icons ${dirArrow}" aria-hidden="true"></span>
                                </button>
                            </div>
                            <div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">
                                ${buildUnratedGrid(paginatedItems)}
                            </div>
                        </div>
                    `;

                    container.querySelectorAll('[data-fallback-step]').forEach(applyImageFallback);
                    fillCompactBadges();
                    attachRateButtonListeners(container);

                    const sortSel = container.querySelector('.sortSelect');
                    if (sortSel) {
                        sortSel.addEventListener('change', (e) => {
                            unratedSortField = e.target.value;
                            unratedSortDir = 'desc';
                            sortUnrated(items, unratedSortField, unratedSortDir);
                            unratedPage = 1;
                            renderPage(1);
                        });
                    }

                    const sdb = container.querySelector('.sortDirBtn');
                    if (sdb) {
                        sdb.addEventListener('click', () => {
                            unratedSortDir = unratedSortDir === 'desc' ? 'asc' : 'desc';
                            sortUnrated(items, unratedSortField, unratedSortDir);
                            unratedPage = 1;
                            renderPage(1);
                        });
                    }

                    const pb = container.querySelector('.prevPageBtn');
                    if (pb && !pb.disabled) {
                        pb.addEventListener('click', () => {
                            unratedPage--;
                            renderPage(unratedPage);
                        });
                    }

                    const nb = container.querySelector('.nextPageBtn');
                    if (nb && !nb.disabled) {
                        nb.addEventListener('click', () => {
                            unratedPage++;
                            renderPage(unratedPage);
                        });
                    }
                }

                renderPage(1);
            }

        } catch (error) {
            console.error('[UserRatings] Error displaying ratings list:', error);
            ratingsTabContent.innerHTML = `
                <div style="padding: 2em; background: rgba(229, 57, 53, 0.2); border: 1px solid rgba(229, 57, 53, 0.5); border-radius: 8px; color: #ff6b6b; margin: 2em;">
                    <strong>Error:</strong> ${error.message}
                </div>
            `;
        }
    }

    // Inject ratings tab on home screen
    function injectRatingsTab() {
        try {
            // Only inject on home page
            if (!window.location.hash.includes('home')) {
                return;
            }
            
            // Check if tab already exists
            const existingTab = document.querySelector('[data-ratings-tab="true"]');
            if (existingTab) {
                return;
            }

            // Try to find the tabs container by locating the Home button first
            const homeButton = Array.from(document.querySelectorAll('.emby-tab-button')).find(btn => 
                btn.textContent.trim().toLowerCase().includes('home')
            );
            
            let tabsSlider = null;
            
            if (homeButton) {
                tabsSlider = homeButton.parentElement;
            } else {
                // Strategy 2: Look for .emby-tabs-slider
                tabsSlider = document.querySelector('.emby-tabs-slider');
            }
            
            if (!tabsSlider) {
                return;
            }

        // Get the next index
        const existingTabs = tabsSlider.querySelectorAll('.emby-tab-button');
        const nextIndex = existingTabs.length;

        // Create the ratings tab button
        const ratingsTab = document.createElement('button');
        ratingsTab.type = 'button';
        ratingsTab.setAttribute('is', 'emby-button');
        ratingsTab.className = 'emby-tab-button emby-button';
        ratingsTab.setAttribute('data-index', nextIndex);
        ratingsTab.setAttribute('data-ratings-tab', 'true');
        ratingsTab.innerHTML = '<div class="emby-button-foreground">User Ratings</div>';

        // Add click handler
        ratingsTab.addEventListener('click', async function(e) {
            e.preventDefault();

            // Skip if already active (no refetch)
            if (ratingsTab.classList.contains('emby-tab-button-active')) {
                return;
            }

            // Remove active class from all tabs
            tabsSlider.querySelectorAll('.emby-tab-button').forEach(tab => {
                tab.classList.remove('emby-tab-button-active');
            });

            // Add active class to this tab
            ratingsTab.classList.add('emby-tab-button-active');

            try {
                // Load and display ratings list in the home page
                await displayRatingsList();
                // Annotate current history entry so back-button restores us
                try {
                    const state = window.history.state || {};
                    state.userRatingsActive = true;
                    window.history.replaceState(state, '', window.location.href);
                } catch (err) {
                    console.warn('[UserRatings] Could not replaceState on tab click:', err);
                }
            } catch (error) {
                console.error('[UserRatings] Error in displayRatingsList:', error);
            }
        });

        // Add listeners to other tabs to properly switch content
        const otherTabs = tabsSlider.querySelectorAll('.emby-tab-button:not([data-ratings-tab="true"])');
        otherTabs.forEach((tab, index) => {
            tab.addEventListener('click', function(e) {
                // Hide ratings tab
                const ratingsTabContent = document.querySelector('#ratingsTab');
                if (ratingsTabContent) {
                    ratingsTabContent.style.display = 'none';
                    ratingsTabContent.classList.add('hide');
                }

                // Show the home page
                const homePage = document.querySelector('[data-role="page"].hide:not(#ratingsTab)');
                if (homePage) {
                    homePage.classList.remove('hide');
                }

                // Clear userRatingsActive so last-clicked native tab wins on back
                try {
                    const state = window.history.state || {};
                    if (state.userRatingsActive) {
                        delete state.userRatingsActive;
                        window.history.replaceState(state, '', window.location.href);
                    }
                } catch (err) { /* ignore */ }
            }, true); // Use capture to run before Jellyfin's handler
        });

            // Insert the tab
            tabsSlider.appendChild(ratingsTab);
            
        } catch (error) {
            console.error('[UserRatings] Tab injection error:', error);
        }
    }

    // Try to inject tab on page load and navigation
    function checkAndInjectTab() {
        injectRatingsTab();
    }

    // Try immediately and repeatedly
    injectRatingsTab();
    setTimeout(injectRatingsTab, 100);
    setTimeout(injectRatingsTab, 500);
    setTimeout(injectRatingsTab, 1000);
    setTimeout(injectRatingsTab, 2000);
    setTimeout(injectRatingsTab, 3000);
    setInterval(injectRatingsTab, 2000);

    // On initial load, if history state says User Ratings was active, restore it (handles refresh)
    setTimeout(() => {
        if (window.location.hash.includes('home')
            && window.history.state
            && window.history.state.userRatingsActive) {
            console.log('[UserRatings] Initial load - restoring User Ratings from state');
            displayRatingsList().catch(() => {});
        }
    }, 800);

    // Watch for page changes
    window.addEventListener('hashchange', () => {
        setTimeout(injectRatingsTab, 100);
        setTimeout(injectRatingsTab, 500);
    });

    // Watch for DOM changes to inject tab
    new MutationObserver(() => {
        injectRatingsTab();
    }).observe(document.body, {
        subtree: true,
        childList: true
    });
})();