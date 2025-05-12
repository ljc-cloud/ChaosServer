/*
 Navicat Premium Data Transfer

 Source Server         : mysql
 Source Server Type    : MySQL
 Source Server Version : 50737
 Source Host           : localhost:3306
 Source Schema         : chaos_ball

 Target Server Type    : MySQL
 Target Server Version : 50737
 File Encoding         : 65001

 Date: 15/04/2025 15:25:03
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for user_data
-- ----------------------------
DROP TABLE IF EXISTS `user_data`;
CREATE TABLE `user_data`  (
  `id` int(20) NOT NULL AUTO_INCREMENT,
  `nick_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `user_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `password` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 20 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of user_data
-- ----------------------------
INSERT INTO `user_data` VALUES (9, '123', '123', '123');
INSERT INTO `user_data` VALUES (10, 'xc', 'xc', '123');
INSERT INTO `user_data` VALUES (11, '11', '11', '11');
INSERT INTO `user_data` VALUES (12, '咸菜', 'xiancai', '123456');
INSERT INTO `user_data` VALUES (13, 'ddd', 'ddd', '123');
INSERT INTO `user_data` VALUES (14, '22', '22', '22');
INSERT INTO `user_data` VALUES (15, '111', '111', '111');
INSERT INTO `user_data` VALUES (16, '1111', '1111', '1111');
INSERT INTO `user_data` VALUES (17, '4444', '4444', '44444');
INSERT INTO `user_data` VALUES (18, '你好', 'hello', '123');
INSERT INTO `user_data` VALUES (19, '222', '222', '222');

SET FOREIGN_KEY_CHECKS = 1;
