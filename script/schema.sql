-- ============================================
-- Schema Base de Donnees - Jeu de Ligne
-- ============================================

-- Table des parties sauvegardees
CREATE TABLE IF NOT EXISTS game_saves (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    board_width INT NOT NULL,
    board_height INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_finished BOOLEAN DEFAULT FALSE,
    winner_id INT NULL,
    auto_save BOOLEAN DEFAULT FALSE
);

-- Table des joueurs dans une partie
CREATE TABLE IF NOT EXISTS game_players (
    id SERIAL PRIMARY KEY,
    game_save_id INT REFERENCES game_saves(id) ON DELETE CASCADE,
    player_id INT NOT NULL,
    name VARCHAR(50) NOT NULL,
    color_r INT NOT NULL,
    color_g INT NOT NULL,
    color_b INT NOT NULL,
    score INT DEFAULT 0,
    cannon_side VARCHAR(10) NOT NULL,
    cannon_position_y INT NOT NULL
);

-- Table des etats de tour (snapshots)
CREATE TABLE IF NOT EXISTS turn_states (
    id SERIAL PRIMARY KEY,
    game_save_id INT REFERENCES game_saves(id) ON DELETE CASCADE,
    turn_number INT NOT NULL,
    current_player_id INT NOT NULL,
    saved_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    action_type VARCHAR(20) NOT NULL,
    description TEXT
);

-- Table des points sur le plateau pour chaque etat
CREATE TABLE IF NOT EXISTS turn_state_points (
    id SERIAL PRIMARY KEY,
    turn_state_id INT REFERENCES turn_states(id) ON DELETE CASCADE,
    x INT NOT NULL,
    y INT NOT NULL,
    owner_player_id INT NOT NULL,
    is_part_of_scored_line BOOLEAN DEFAULT FALSE
);

-- Table des directions scorees pour chaque point
CREATE TABLE IF NOT EXISTS point_scored_directions (
    id SERIAL PRIMARY KEY,
    turn_state_point_id INT REFERENCES turn_state_points(id) ON DELETE CASCADE,
    dx INT NOT NULL,
    dy INT NOT NULL
);

-- Table des lignes scorees pour chaque etat
CREATE TABLE IF NOT EXISTS turn_state_lines (
    id SERIAL PRIMARY KEY,
    turn_state_id INT REFERENCES turn_states(id) ON DELETE CASCADE,
    owner_player_id INT NOT NULL,
    is_scored BOOLEAN DEFAULT TRUE
);

-- Table des points des lignes scorees
CREATE TABLE IF NOT EXISTS line_points (
    id SERIAL PRIMARY KEY,
    turn_state_line_id INT REFERENCES turn_state_lines(id) ON DELETE CASCADE,
    x INT NOT NULL,
    y INT NOT NULL,
    point_order INT NOT NULL
);

-- Table des scores des joueurs pour chaque tour
CREATE TABLE IF NOT EXISTS turn_state_scores (
    id SERIAL PRIMARY KEY,
    turn_state_id INT REFERENCES turn_states(id) ON DELETE CASCADE,
    player_id INT NOT NULL,
    score INT NOT NULL
);

-- ============================================
-- Index pour ameliorer les performances
-- ============================================

CREATE INDEX IF NOT EXISTS idx_turn_states_game ON turn_states(game_save_id);
CREATE INDEX IF NOT EXISTS idx_turn_state_points_turn ON turn_state_points(turn_state_id);
CREATE INDEX IF NOT EXISTS idx_turn_state_lines_turn ON turn_state_lines(turn_state_id);
