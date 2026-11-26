-- phpMyAdmin SQL Dump
-- version 5.2.0
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1:3306
-- Время создания: Ноя 23 2026 г., 12:54
-- Версия сервера: 8.0.30
-- Версия PHP: 7.2.34

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- База данных: `coffeecare`
--

-- --------------------------------------------------------

--
-- Структура таблицы `checklist_items`
--

CREATE TABLE `checklist_items` (
  `id` int NOT NULL,
  `protocol_id` int NOT NULL,
  `name` varchar(200) NOT NULL,
  `is_checked` tinyint(1) NOT NULL DEFAULT '0',
  `sort_order` int NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `coffee_machines`
--

CREATE TABLE `coffee_machines` (
  `id` int NOT NULL,
  `model_id` int NOT NULL,
  `location_id` int NOT NULL,
  `serial_number` varchar(100) NOT NULL,
  `inventory_number` varchar(50) NOT NULL,
  `status` varchar(30) NOT NULL DEFAULT 'working',
  `installed_at` date DEFAULT NULL,
  `last_seen_at` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `drinks`
--

CREATE TABLE `drinks` (
  `id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `volume_ml` int NOT NULL,
  `price` decimal(12,2) NOT NULL,
  `description` text,
  `is_active` tinyint(1) NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `ingredients`
--

CREATE TABLE `ingredients` (
  `id` int NOT NULL,
  `article` varchar(50) NOT NULL,
  `name` varchar(100) NOT NULL,
  `unit` varchar(10) NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `inventories`
--

CREATE TABLE `inventories` (
  `id` int NOT NULL,
  `number` varchar(50) NOT NULL,
  `location_id` int NOT NULL,
  `created_by` int NOT NULL,
  `status` varchar(20) NOT NULL DEFAULT 'draft',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `completed_at` datetime DEFAULT NULL,
  `comment` text
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `inventory_items`
--

CREATE TABLE `inventory_items` (
  `id` int NOT NULL,
  `inventory_id` int NOT NULL,
  `ingredient_id` int NOT NULL,
  `expected_quantity` decimal(12,3) NOT NULL,
  `actual_quantity` decimal(12,3) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `locations`
--

CREATE TABLE `locations` (
  `id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `address` varchar(255) NOT NULL,
  `opening_hours` varchar(150) DEFAULT NULL,
  `description` text,
  `is_active` tinyint(1) NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `machine_models`
--

CREATE TABLE `machine_models` (
  `id` int NOT NULL,
  `manufacturer` varchar(100) NOT NULL,
  `name` varchar(100) NOT NULL,
  `maintenance_interval_days` int NOT NULL DEFAULT '30',
  `description` text
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `maintenance_plans`
--

CREATE TABLE `maintenance_plans` (
  `id` int NOT NULL,
  `machine_id` int NOT NULL,
  `maintenance_type` varchar(30) NOT NULL,
  `planned_date` date NOT NULL,
  `status` varchar(20) NOT NULL DEFAULT 'planned',
  `comment` text
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `notes`
--

CREATE TABLE `notes` (
  `id` int NOT NULL,
  `author_id` int NOT NULL,
  `work_order_id` int NOT NULL,
  `title` varchar(150) NOT NULL,
  `body` text NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `notifications`
--

CREATE TABLE `notifications` (
  `id` int NOT NULL,
  `user_id` int NOT NULL,
  `work_order_id` int DEFAULT NULL,
  `location_id` int DEFAULT NULL,
  `type` varchar(40) NOT NULL,
  `title` varchar(150) NOT NULL,
  `message` text NOT NULL,
  `is_read` tinyint(1) NOT NULL DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `parts_usage`
--

CREATE TABLE `parts_usage` (
  `id` int NOT NULL,
  `protocol_id` int NOT NULL,
  `part_id` int NOT NULL,
  `quantity` int NOT NULL,
  `unit_cost` decimal(12,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `protocols`
--

CREATE TABLE `protocols` (
  `id` int NOT NULL,
  `work_order_id` int NOT NULL,
  `performed_by` int NOT NULL,
  `performed_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `counter_value` int DEFAULT NULL,
  `works_description` text NOT NULL,
  `result` varchar(30) NOT NULL,
  `labor_cost` decimal(12,2) NOT NULL DEFAULT '0.00',
  `photo_path` varchar(255) DEFAULT NULL,
  `signature_path` varchar(255) DEFAULT NULL,
  `pdf_path` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `recipe_items`
--

CREATE TABLE `recipe_items` (
  `id` int NOT NULL,
  `drink_id` int NOT NULL,
  `ingredient_id` int NOT NULL,
  `quantity` decimal(12,3) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `roles`
--

CREATE TABLE `roles` (
  `id` int NOT NULL,
  `code` varchar(30) NOT NULL,
  `name` varchar(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Дамп данных таблицы `roles`
--

INSERT INTO `roles` (`id`, `code`, `name`) VALUES
(1, 'admin', 'Администратор'),
(2, 'dispatcher', 'Диспетчер'),
(3, 'manager', 'Менеджер'),
(4, 'technician', 'Техник');

-- --------------------------------------------------------

--
-- Структура таблицы `sales`
--

CREATE TABLE `sales` (
  `id` int NOT NULL,
  `number` varchar(50) NOT NULL,
  `machine_id` int NOT NULL,
  `location_id` int NOT NULL,
  `sold_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `status` varchar(20) NOT NULL DEFAULT 'completed'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `sale_items`
--

CREATE TABLE `sale_items` (
  `id` int NOT NULL,
  `sale_id` int NOT NULL,
  `drink_id` int NOT NULL,
  `quantity` int NOT NULL DEFAULT '1',
  `unit_price` decimal(12,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `spare_parts`
--

CREATE TABLE `spare_parts` (
  `id` int NOT NULL,
  `article` varchar(50) NOT NULL,
  `name` varchar(150) NOT NULL,
  `quantity` int NOT NULL DEFAULT '0',
  `min_quantity` int NOT NULL DEFAULT '0',
  `unit_cost` decimal(12,2) NOT NULL DEFAULT '0.00',
  `is_active` tinyint(1) NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `stock_balances`
--

CREATE TABLE `stock_balances` (
  `id` int NOT NULL,
  `location_id` int NOT NULL,
  `ingredient_id` int NOT NULL,
  `quantity` decimal(12,3) NOT NULL DEFAULT '0.000',
  `min_quantity` decimal(12,3) NOT NULL DEFAULT '0.000'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `stock_movements`
--

CREATE TABLE `stock_movements` (
  `id` int NOT NULL,
  `location_id` int NOT NULL,
  `ingredient_id` int NOT NULL,
  `user_id` int NOT NULL,
  `sale_id` int DEFAULT NULL,
  `inventory_item_id` int DEFAULT NULL,
  `movement_type` varchar(30) NOT NULL,
  `quantity` decimal(12,3) NOT NULL,
  `reason` text,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `users`
--

CREATE TABLE `users` (
  `id` int NOT NULL,
  `role_id` int NOT NULL,
  `full_name` varchar(150) NOT NULL,
  `email` varchar(150) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `phone` varchar(30) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `work_orders`
--

CREATE TABLE `work_orders` (
  `id` int NOT NULL,
  `number` varchar(50) NOT NULL,
  `machine_id` int NOT NULL,
  `maintenance_plan_id` int DEFAULT NULL,
  `technician_id` int DEFAULT NULL,
  `created_by` int NOT NULL,
  `maintenance_type` varchar(30) NOT NULL,
  `status` varchar(30) NOT NULL DEFAULT 'new',
  `priority` int NOT NULL DEFAULT '2',
  `description` text NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `due_at` datetime NOT NULL,
  `scheduled_start` datetime DEFAULT NULL,
  `scheduled_end` datetime DEFAULT NULL,
  `cancellation_reason` text
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Структура таблицы `work_order_history`
--

CREATE TABLE `work_order_history` (
  `id` int NOT NULL,
  `work_order_id` int NOT NULL,
  `changed_by` int NOT NULL,
  `old_status` varchar(30) DEFAULT NULL,
  `new_status` varchar(30) NOT NULL,
  `reason` text,
  `changed_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Индексы сохранённых таблиц
--

--
-- Индексы таблицы `checklist_items`
--
ALTER TABLE `checklist_items`
  ADD PRIMARY KEY (`id`),
  ADD KEY `protocol_id` (`protocol_id`);

--
-- Индексы таблицы `coffee_machines`
--
ALTER TABLE `coffee_machines`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `serial_number` (`serial_number`),
  ADD UNIQUE KEY `inventory_number` (`inventory_number`),
  ADD KEY `model_id` (`model_id`),
  ADD KEY `location_id` (`location_id`);

--
-- Индексы таблицы `drinks`
--
ALTER TABLE `drinks`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `name` (`name`,`volume_ml`);

--
-- Индексы таблицы `ingredients`
--
ALTER TABLE `ingredients`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `article` (`article`);

--
-- Индексы таблицы `inventories`
--
ALTER TABLE `inventories`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `number` (`number`),
  ADD KEY `location_id` (`location_id`),
  ADD KEY `created_by` (`created_by`);

--
-- Индексы таблицы `inventory_items`
--
ALTER TABLE `inventory_items`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `inventory_id` (`inventory_id`,`ingredient_id`),
  ADD KEY `ingredient_id` (`ingredient_id`);

--
-- Индексы таблицы `locations`
--
ALTER TABLE `locations`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `name` (`name`);

--
-- Индексы таблицы `machine_models`
--
ALTER TABLE `machine_models`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `manufacturer` (`manufacturer`,`name`);

--
-- Индексы таблицы `maintenance_plans`
--
ALTER TABLE `maintenance_plans`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `machine_id` (`machine_id`,`maintenance_type`,`planned_date`);

--
-- Индексы таблицы `notes`
--
ALTER TABLE `notes`
  ADD PRIMARY KEY (`id`),
  ADD KEY `author_id` (`author_id`),
  ADD KEY `work_order_id` (`work_order_id`);

--
-- Индексы таблицы `notifications`
--
ALTER TABLE `notifications`
  ADD PRIMARY KEY (`id`),
  ADD KEY `user_id` (`user_id`),
  ADD KEY `work_order_id` (`work_order_id`),
  ADD KEY `location_id` (`location_id`);

--
-- Индексы таблицы `parts_usage`
--
ALTER TABLE `parts_usage`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `protocol_id` (`protocol_id`,`part_id`),
  ADD KEY `part_id` (`part_id`);

--
-- Индексы таблицы `protocols`
--
ALTER TABLE `protocols`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `work_order_id` (`work_order_id`),
  ADD KEY `performed_by` (`performed_by`);

--
-- Индексы таблицы `recipe_items`
--
ALTER TABLE `recipe_items`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `drink_id` (`drink_id`,`ingredient_id`),
  ADD KEY `ingredient_id` (`ingredient_id`);

--
-- Индексы таблицы `roles`
--
ALTER TABLE `roles`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `code` (`code`);

--
-- Индексы таблицы `sales`
--
ALTER TABLE `sales`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `number` (`number`),
  ADD KEY `machine_id` (`machine_id`),
  ADD KEY `location_id` (`location_id`);

--
-- Индексы таблицы `sale_items`
--
ALTER TABLE `sale_items`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `sale_id` (`sale_id`,`drink_id`),
  ADD KEY `drink_id` (`drink_id`);

--
-- Индексы таблицы `spare_parts`
--
ALTER TABLE `spare_parts`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `article` (`article`);

--
-- Индексы таблицы `stock_balances`
--
ALTER TABLE `stock_balances`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `location_id` (`location_id`,`ingredient_id`),
  ADD KEY `ingredient_id` (`ingredient_id`);

--
-- Индексы таблицы `stock_movements`
--
ALTER TABLE `stock_movements`
  ADD PRIMARY KEY (`id`),
  ADD KEY `location_id` (`location_id`),
  ADD KEY `ingredient_id` (`ingredient_id`),
  ADD KEY `user_id` (`user_id`),
  ADD KEY `sale_id` (`sale_id`),
  ADD KEY `inventory_item_id` (`inventory_item_id`);

--
-- Индексы таблицы `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `email` (`email`),
  ADD KEY `role_id` (`role_id`);

--
-- Индексы таблицы `work_orders`
--
ALTER TABLE `work_orders`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `number` (`number`),
  ADD UNIQUE KEY `maintenance_plan_id` (`maintenance_plan_id`),
  ADD KEY `machine_id` (`machine_id`),
  ADD KEY `technician_id` (`technician_id`),
  ADD KEY `created_by` (`created_by`);

--
-- Индексы таблицы `work_order_history`
--
ALTER TABLE `work_order_history`
  ADD PRIMARY KEY (`id`),
  ADD KEY `work_order_id` (`work_order_id`),
  ADD KEY `changed_by` (`changed_by`);

--
-- AUTO_INCREMENT для сохранённых таблиц
--

--
-- AUTO_INCREMENT для таблицы `checklist_items`
--
ALTER TABLE `checklist_items`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `coffee_machines`
--
ALTER TABLE `coffee_machines`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `drinks`
--
ALTER TABLE `drinks`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `ingredients`
--
ALTER TABLE `ingredients`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `inventories`
--
ALTER TABLE `inventories`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `inventory_items`
--
ALTER TABLE `inventory_items`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `locations`
--
ALTER TABLE `locations`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `machine_models`
--
ALTER TABLE `machine_models`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `maintenance_plans`
--
ALTER TABLE `maintenance_plans`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `notes`
--
ALTER TABLE `notes`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `notifications`
--
ALTER TABLE `notifications`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `parts_usage`
--
ALTER TABLE `parts_usage`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `protocols`
--
ALTER TABLE `protocols`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `recipe_items`
--
ALTER TABLE `recipe_items`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `roles`
--
ALTER TABLE `roles`
  MODIFY `id` int NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT для таблицы `sales`
--
ALTER TABLE `sales`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `sale_items`
--
ALTER TABLE `sale_items`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `spare_parts`
--
ALTER TABLE `spare_parts`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `stock_balances`
--
ALTER TABLE `stock_balances`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `stock_movements`
--
ALTER TABLE `stock_movements`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `users`
--
ALTER TABLE `users`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `work_orders`
--
ALTER TABLE `work_orders`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `work_order_history`
--
ALTER TABLE `work_order_history`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- Ограничения внешнего ключа сохраненных таблиц
--

--
-- Ограничения внешнего ключа таблицы `checklist_items`
--
ALTER TABLE `checklist_items`
  ADD CONSTRAINT `checklist_items_ibfk_1` FOREIGN KEY (`protocol_id`) REFERENCES `protocols` (`id`);

--
-- Ограничения внешнего ключа таблицы `coffee_machines`
--
ALTER TABLE `coffee_machines`
  ADD CONSTRAINT `coffee_machines_ibfk_1` FOREIGN KEY (`model_id`) REFERENCES `machine_models` (`id`),
  ADD CONSTRAINT `coffee_machines_ibfk_2` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`);

--
-- Ограничения внешнего ключа таблицы `inventories`
--
ALTER TABLE `inventories`
  ADD CONSTRAINT `inventories_ibfk_1` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`),
  ADD CONSTRAINT `inventories_ibfk_2` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`);

--
-- Ограничения внешнего ключа таблицы `inventory_items`
--
ALTER TABLE `inventory_items`
  ADD CONSTRAINT `inventory_items_ibfk_1` FOREIGN KEY (`inventory_id`) REFERENCES `inventories` (`id`),
  ADD CONSTRAINT `inventory_items_ibfk_2` FOREIGN KEY (`ingredient_id`) REFERENCES `ingredients` (`id`);

--
-- Ограничения внешнего ключа таблицы `maintenance_plans`
--
ALTER TABLE `maintenance_plans`
  ADD CONSTRAINT `maintenance_plans_ibfk_1` FOREIGN KEY (`machine_id`) REFERENCES `coffee_machines` (`id`);

--
-- Ограничения внешнего ключа таблицы `notes`
--
ALTER TABLE `notes`
  ADD CONSTRAINT `notes_ibfk_1` FOREIGN KEY (`author_id`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `notes_ibfk_2` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`);

--
-- Ограничения внешнего ключа таблицы `notifications`
--
ALTER TABLE `notifications`
  ADD CONSTRAINT `notifications_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `notifications_ibfk_2` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`),
  ADD CONSTRAINT `notifications_ibfk_3` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`);

--
-- Ограничения внешнего ключа таблицы `parts_usage`
--
ALTER TABLE `parts_usage`
  ADD CONSTRAINT `parts_usage_ibfk_1` FOREIGN KEY (`protocol_id`) REFERENCES `protocols` (`id`),
  ADD CONSTRAINT `parts_usage_ibfk_2` FOREIGN KEY (`part_id`) REFERENCES `spare_parts` (`id`);

--
-- Ограничения внешнего ключа таблицы `protocols`
--
ALTER TABLE `protocols`
  ADD CONSTRAINT `protocols_ibfk_1` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`),
  ADD CONSTRAINT `protocols_ibfk_2` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`);

--
-- Ограничения внешнего ключа таблицы `recipe_items`
--
ALTER TABLE `recipe_items`
  ADD CONSTRAINT `recipe_items_ibfk_1` FOREIGN KEY (`drink_id`) REFERENCES `drinks` (`id`),
  ADD CONSTRAINT `recipe_items_ibfk_2` FOREIGN KEY (`ingredient_id`) REFERENCES `ingredients` (`id`);

--
-- Ограничения внешнего ключа таблицы `sales`
--
ALTER TABLE `sales`
  ADD CONSTRAINT `sales_ibfk_1` FOREIGN KEY (`machine_id`) REFERENCES `coffee_machines` (`id`),
  ADD CONSTRAINT `sales_ibfk_2` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`);

--
-- Ограничения внешнего ключа таблицы `sale_items`
--
ALTER TABLE `sale_items`
  ADD CONSTRAINT `sale_items_ibfk_1` FOREIGN KEY (`sale_id`) REFERENCES `sales` (`id`),
  ADD CONSTRAINT `sale_items_ibfk_2` FOREIGN KEY (`drink_id`) REFERENCES `drinks` (`id`);

--
-- Ограничения внешнего ключа таблицы `stock_balances`
--
ALTER TABLE `stock_balances`
  ADD CONSTRAINT `stock_balances_ibfk_1` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`),
  ADD CONSTRAINT `stock_balances_ibfk_2` FOREIGN KEY (`ingredient_id`) REFERENCES `ingredients` (`id`);

--
-- Ограничения внешнего ключа таблицы `stock_movements`
--
ALTER TABLE `stock_movements`
  ADD CONSTRAINT `stock_movements_ibfk_1` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`),
  ADD CONSTRAINT `stock_movements_ibfk_2` FOREIGN KEY (`ingredient_id`) REFERENCES `ingredients` (`id`),
  ADD CONSTRAINT `stock_movements_ibfk_3` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `stock_movements_ibfk_4` FOREIGN KEY (`sale_id`) REFERENCES `sales` (`id`),
  ADD CONSTRAINT `stock_movements_ibfk_5` FOREIGN KEY (`inventory_item_id`) REFERENCES `inventory_items` (`id`);

--
-- Ограничения внешнего ключа таблицы `users`
--
ALTER TABLE `users`
  ADD CONSTRAINT `users_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`);

--
-- Ограничения внешнего ключа таблицы `work_orders`
--
ALTER TABLE `work_orders`
  ADD CONSTRAINT `work_orders_ibfk_1` FOREIGN KEY (`machine_id`) REFERENCES `coffee_machines` (`id`),
  ADD CONSTRAINT `work_orders_ibfk_2` FOREIGN KEY (`maintenance_plan_id`) REFERENCES `maintenance_plans` (`id`),
  ADD CONSTRAINT `work_orders_ibfk_3` FOREIGN KEY (`technician_id`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `work_orders_ibfk_4` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`);

--
-- Ограничения внешнего ключа таблицы `work_order_history`
--
ALTER TABLE `work_order_history`
  ADD CONSTRAINT `work_order_history_ibfk_1` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`),
  ADD CONSTRAINT `work_order_history_ibfk_2` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
